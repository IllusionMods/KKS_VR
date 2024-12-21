//using System.Linq;
//using UnityEngine;
//using VRGIN.Controls;
//using VRGIN.Core;


// Descriptions are gone.
//namespace KKS_VR.Controls
//{
//    internal class ToolUtil
//    {
//        public static HelpText HelpTrigger(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(0.06f, -0.04f, -0.05f), Vector3.zero, "trigger");
//        }

//        public static HelpText HelpGrip(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(-0.06f, 0, -0.05f), Vector3.zero, "lgrip", "handgrip");
//        }

//        public static HelpText HelpTrackpadCenter(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(0, 0.06f, 0.02f), Vector3.zero, "trackpad", "thumbstick");
//        }

//        public static HelpText HelpTrackpadLeft(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(-0.05f, 0.04f, 0), new Vector3(-0.01f, 0, 0), "trackpad", "thumbstick");
//        }

//        public static HelpText HelpTrackpadRight(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(0.05f, 0.04f, 0), new Vector3(0.01f, 0, 0), "trackpad", "thumbstick");
//        }

//        public static HelpText HelpTrackpadUp(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(0, 0.04f, 0.07f), new Vector3(0, 0, 0.01f), "trackpad", "thumbstick");
//        }

//        public static HelpText HelpTrackpadDown(Controller controller, string description)
//        {
//            return MakeHelpText(controller, description, new Vector3(0, 0.04f, -0.05f), new Vector3(0, 0, -0.01f), "trackpad", "thumbstick");
//        }


//        public static HelpText MakeHelpText(
//            Controller controller,
//            string description,
//            Vector3 textOffset,
//            Vector3 lineOffset,
//            params string[] attachNames)
//        {
//            var attach = attachNames
//                .Select(name => controller.FindAttachPosition(name))
//                .Where(x => x != null)
//                .FirstOrDefault();
//            if (attach == null)
//            {
//                VRLog.Warn($"HelpText: attach point not found for {attachNames}");
//                return null;
//            }

//            return HelpText.Create(description, attach, textOffset, lineOffset);
//        }
//    }
//}
