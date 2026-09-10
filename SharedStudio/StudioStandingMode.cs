using System;
using System.Collections.Generic;
using System.Linq;
using KK_VR.Controls;
using KK_VR.Features;
using KK_VR.Settings;
using VRGIN.Controls;
using VRGIN.Modes;

namespace KK_VR
{
    internal class StudioStandingMode : StandingMode
    {
        public override IEnumerable<Type> Tools { get; } = new[]
        {
            typeof(Controls.GameplayTool)
        };

        protected override IEnumerable<IShortcut> CreateShortcuts()
        {
            // Disable all VRGIN shortcuts. We'll define necessary shortcuts
            // (if any) by ourselves.
            return Enumerable.Empty<IShortcut>();
        }
        protected override Controller CreateLeftController()
        {
            var controller = base.CreateLeftController();
            controller.ToolIndex = 0;
            return controller;
        }

        protected override Controller CreateRightController()
        {
            var controller = base.CreateRightController();
            controller.ToolIndex = 0;
            return controller;
        }

    }
}
