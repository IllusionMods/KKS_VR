using KKAPI.MainGame;
using KK_VR.Interpreters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Illusion.Component.ShortcutKey;

namespace KK_VR.Interpreters
{
    // Because I'd much rather use neat hooks instead of re-inventing them.
    internal class InterpreterHooks : GameCustomFunctionController
    {
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            KoikatuInterpreter.StartScene(KoikatuInterpreter.SceneType.HScene, proc);
        }
        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr)
        {
            KoikatuInterpreter.EndScene(KoikatuInterpreter.SceneType.HScene);
        }
    }
}
