using System;

namespace Antchemy
{
    // Opportunities are consumed at the transition, never when a late network response arrives.
    public sealed class AppOpenPolicy
    {
        readonly Func<double> now;
        readonly double minimumBackground, launchWait;
        readonly bool launchEnabled;
        double launchDeadline;
        double backgroundAt;
        bool backgrounded, blockedBackground;
        public bool LaunchPending { get; private set; }

        public AppOpenPolicy(AdConfiguration config, Func<double> now, bool allowLaunch = true)
        {
            this.now=now;
            launchEnabled=config.appOpenEnabled;
            launchWait=Math.Max(0,Math.Min(4,config.appOpenLaunchWaitSeconds));
            LaunchPending=launchEnabled && allowLaunch;
            launchDeadline=now()+launchWait;
            minimumBackground=Math.Max(0,config.appOpenMinimumBackgroundSeconds);
        }

        // Opens the cold-start window once the welcome splash has finished and the ads were preloaded behind it.
        public void BeginLaunch()
        {
            if(!launchEnabled || backgrounded) return;
            LaunchPending=true; launchDeadline=now()+launchWait;
        }

        public bool TakeLaunch(bool ready, bool blocked)
        {
            if(!LaunchPending) return false;
            if(now()>=launchDeadline) { LaunchPending=false; return false; }
            if(blocked || !ready) return false;
            LaunchPending=false; return true;
        }

        public void Background(bool blocked)
        {
            LaunchPending=false;
            if(backgrounded) { blockedBackground |= blocked; return; }
            backgrounded=true; backgroundAt=now(); blockedBackground=blocked;
        }

        public bool TakeForeground(bool blocked)
        {
            if(!backgrounded) return false;
            backgrounded=false;
            return !blocked && !blockedBackground && now()-backgroundAt>=minimumBackground;
        }
    }
}
