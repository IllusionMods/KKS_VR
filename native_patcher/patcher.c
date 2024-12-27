/* A runtime patcher for Koikatu.exe
 *
 * This code is meant to be compiled into a DLL file and loaded onto a Koikatu
 * process. It then applies some patches to the native code of Koikatu.exe in
 * memory.
 *
 * Currently it makes only one change: when the process reads the file
 * Koikatu_Data/globalgamemanagers to load the Unity build settings, it is
 * changed to behave as if the "enabledVRDevices" field contained the two
 * strings "None" and "OpenVR" in this order, regardless of what is actually
 * specified in the file.
 */


#include <windows.h>

#ifdef _VERBOSE
HANDLE log_handle;
char buffer[4096];

inline void init_logger() {
    log_handle = CreateFileA("maingamevr_patcher.log", GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL,
        NULL);
}

inline void free_logger() { CloseHandle(log_handle); }

#define LOG(message, ...) \
	{ \
		size_t len = wsprintfA(buffer, message, __VA_ARGS__); \
		WriteFile(log_handle, buffer, len, NULL, NULL); \
	}

#else
inline void init_logger() {
}

inline void free_logger() {
}

#define LOG(message, ...)
#endif

/* This is just a memcpy, but circumvents memory protection. */
BOOL patch(BYTE *old_code, const BYTE *new_code, size_t len)
{
    DWORD old_protect;
    if (!VirtualProtect(old_code, len, PAGE_EXECUTE_READWRITE, &old_protect))
    {
        LOG("Failed to make code writable\n");
        return FALSE;
    }

    memcpy(old_code, new_code, len);

    VirtualProtect(old_code, len, old_protect, &old_protect);
    return TRUE;
}

/* Patch after checking that the code being patched is what we expect. */
BOOL check_and_patch(BYTE *code, const BYTE *expected_code, const BYTE *new_code, size_t len)
{
    if (memcmp(code, expected_code, len))
    {
        LOG("Found unexpected code\n");
        return FALSE;
    }

    return patch(code, new_code, len);
}

/* The patching plan
 *
 * At base+0x4c4730 there is a function that parses Unity build settings. It
 * makes several calls to a function that we call parse_string_array. One of the
 * calls is responsible for parsing the enabledVRDevices field. Our plan is to
 * replace it with a call to a function we define,
 * parse_string_array_and_inject_openvr.
 *
 * However, we can't simply patch the original code to call our function,
 * because our function (which resides in this DLL) is more than 2GB away from
 * the code being patched in the address space, meaning that it can't be called
 * with the usual instruction.
 *
 * For this reason we create a small indirection function near the code being
 * patched, and patch the code to call that function instead. The indirection
 * function simply tail-calls our function, and is 12-bytes long, so it can fit
 * within a padding space between 16-byte-aligned functions in the original
 * executable.
 */

/* The base address of the .exe module */
void *main_module_base;

/* parse_string_array(parser *, vector<string> *) */
#define PARSE_STRING_ARRAY ((void (*)(void *, void **))((BYTE *)main_module_base + 0xab780))
/* vector<string>::resize(size_t) */
#define RESIZE_VECTOR_OF_STRINGS ((void (*)(void **, size_t))((BYTE *)main_module_base + 0xa92c0))
/* string::assign(const char *, size_t) */
#define STRING_ASSIGN ((void (*)(void *, const char *, size_t))((BYTE *)main_module_base + 0x47030))

/* The function to be called from patched code.
 *
 * It parses an array of strings, but instead of returning the parsed value,
 * it always returns the two-element array ["None", "OpenVR"].
 */
void parse_string_array_and_inject_openvr(void *parser, void **dest)
{
    LOG("parse_string_array_and_inject_openvr\n");

    /* First we call the original function to make sure that the internal state
     * of the parser is correctly advanced. This sets *dest, a vector of
     * strings.
     */

    PARSE_STRING_ARRAY(parser, dest);

    LOG("original function returned %d items (%d bytes)\n",
        (int)(((BYTE *)dest[1] - (BYTE *)dest[0]) / 40),
        (int)((BYTE *)dest[1] - (BYTE *)dest[0]));

    /* Resize *dest. */

    RESIZE_VECTOR_OF_STRINGS(dest, 2);

    LOG("after allocation we have %d items (%d bytes)\n",
        (int)(((BYTE *)dest[1] - (BYTE *)dest[0]) / 40),
        (int)((BYTE *)dest[1] - (BYTE *)dest[0]));

    /* Set strings */

    STRING_ASSIGN(dest[0], "None", 4);
    STRING_ASSIGN((BYTE *)dest[0] + 40, "OpenVR", 6);
}

const BYTE expected_parser_calling_code[] = {
    /* lea rdx, [rdi+90h] */
    0x48, 0x8d, 0x97, 0x90, 0x00, 0x00, 0x00,
    /* xor r8d, r8d */
    0x45, 0x33, 0xc0,
    /* mov rcx, rbp */
    0x48, 0x8b, 0xcd,
    /* call parse_string_array */
    0xe8, 0xfe, 0x6f, 0xbe, 0xff
};

const BYTE new_parser_calling_code[sizeof(expected_parser_calling_code)] = {
    /* lea rdx, [rdi+90h] */
    0x48, 0x8d, 0x97, 0x90, 0x00, 0x00, 0x00,
    /* xor r8d, r8d */
    0x45, 0x33, 0xc0,
    /* mov rcx, rbp */
    0x48, 0x8b, 0xcd,
    /* call indirection */
    0xe8, 0x71, 0xf9, 0xff, 0xff
};

const BYTE expected_indirection_code[] = {
    0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc
};

/* Perform all patching. */
BOOL patch_all()
{
    main_module_base = GetModuleHandleA(NULL);
    LOG("base address = %p\n", main_module_base);

    BYTE *indirection_code = (BYTE *)main_module_base + 0x4c40f3;

    BYTE new_indirection_code[sizeof(expected_indirection_code)] = {
        /* mov rax, XXXXXXXXXXXXXXXX */
        0x48, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0,
        /* jmp rax */
        0xff, 0xe0,
        /* int3 */
        0xcc
    };
    void *const injector_address = parse_string_array_and_inject_openvr;
    memcpy(new_indirection_code + 2, &injector_address, sizeof(void *));

    if (!check_and_patch(
        indirection_code,
        expected_indirection_code,
        new_indirection_code,
        sizeof(new_indirection_code)))
    {
        return FALSE;
    }

    BYTE *parser_calling_code = (BYTE *)main_module_base + 0x4c4770;

    if (!check_and_patch(
        parser_calling_code,
        expected_parser_calling_code,
        new_parser_calling_code,
        sizeof(expected_parser_calling_code)))
    {
        return FALSE;
    }

    return TRUE;
}

/* The entry point of the DLL. */
void setup_all()
{
    LOG("Patcher started!\n");

    if (!patch_all()) {
        LOG("Failed to patch game!\n");
        free_logger();
    }
    else {
        LOG("Patch successful!\n");
    }

}

BOOL APIENTRY DllMain(HINSTANCE hInstDll, DWORD reasonForDllLoad, LPVOID reserved)
{
    if (reasonForDllLoad != DLL_PROCESS_ATTACH)
        return TRUE;

    init_logger();

    return TRUE;
}
