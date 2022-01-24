using System;
using System.Collections.Generic;
using System.Linq;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;

namespace KKSCharaStudioVR
{
	internal class GenericStandingMode : StandingMode
	{
		public override IEnumerable<Type> Tools => base.Tools.Concat(new Type[3]
		{
			typeof(MenuTool),
			typeof(WarpTool),
			typeof(GripMoveStudioNEOV2Tool)
		});

		protected override IEnumerable<IShortcut> CreateShortcuts()
		{
			return base.CreateShortcuts().Concat(new IShortcut[1]
			{
				new MultiKeyboardShortcut(new KeyStroke("Ctrl+C"), new KeyStroke("Ctrl+C"), delegate
				{
					VR.Manager.SetMode<GenericSeatedMode>();
				})
			});
		}
	}
}
