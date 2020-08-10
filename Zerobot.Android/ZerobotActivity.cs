using Android.App;
using Android.OS;
using Android.Content.PM;

using Stride.Engine;
using Stride.Starter;

namespace Zerobot
{
    [Activity(MainLauncher = true, 
              Icon = "@drawable/icon", 
              ScreenOrientation = ScreenOrientation.Landscape,
              ConfigurationChanges = ConfigChanges.UiMode | ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class ZerobotActivity : AndroidStrideActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Game = new Game();
            Game.Run(GameContext);
        }

        protected override void OnDestroy()
        {
            Game.Dispose();

            base.OnDestroy();
        }
    }
}
