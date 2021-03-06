using System.Numerics;

using JankWorks.Graphics;

using JankWorks.Game;
using JankWorks.Game.Assets;
using JankWorks.Game.Diagnostics;
using JankWorks.Game.Local;

using JankWorks.Game.Hosting;

using Pong.Match.Players;
using Pong.Match.Physics;

namespace Pong.Match
{
    sealed class MatchScene : Scene
    {
        private MatchState state;
        private Bounds area;

        private PlayerSystem players;
        private PhysicsSystem physics;

        private Ball ball;


        public override void PreInitialise(object state)
        {            
            this.state = state as MatchState ?? new MatchState();
            this.area = new Bounds(Vector2.Zero, new Vector2(1024, 768));
            base.PreInitialise(state);
        }

        public override void HostInitialise(Host host)
        {
            this.physics = new PhysicsSystem(this.area);

            this.players = new PlayerSystem(this.physics, 2);
            this.ball = new Ball(this.physics, area, this.players.PlayerSize.X * 0.75f);

            this.players.RegisterPlayer(0);
            this.players.RegisterPlayer(1);

            if (this.state.PlayerOne == PlayerType.Bot)
            {
                this.RegisterHostObject(new BotPlayer(0, this.physics, this.players, this.ball, this.area));
            }

            if (this.state.PlayerTwo == PlayerType.Bot)
            {
                this.RegisterHostObject(new BotPlayer(1, this.physics, this.players, this.ball, this.area));
            }

            this.RegisterHostObject(this.players);
            this.RegisterHostObject(this.ball);
            this.RegisterHostObject(this.physics);

            base.HostInitialise(host);
        }

        public override void SharedInitialise(Host host, Client client)
        {            
            if (this.state.PlayerOne == PlayerType.Local)
            {
                this.RegisterClientObject(new InputPlayer(0));
            }
            else if (this.state.PlayerTwo == PlayerType.Local)
            {
                this.RegisterClientObject(new InputPlayer(1));
            }

            this.RegisterClientObject(new PhysicsRenderer());
           
            var perfinfo = new PerformanceInfo(client, host, new Asset("embedded", "ibm-plex-mono.regular.ttf"), 14);
            perfinfo.MaxDisplayCounters = 8;
            perfinfo.Colour = Pong.UI.Colours.Hover;
            this.RegisterClientObject(perfinfo);

            base.SharedInitialise(host, client);
        }

        public override void SharedInitialised(object state)
        {
            var playerSize = this.players.PlayerSize;
            var playerYStart = (this.area.BottomRight.Y / 2);

            this.players.SetPlayerPosition(0, new Vector2(0, playerYStart), new Vector2(0, 0.5f));
            this.players.SetPlayerPosition(1, new Vector2(this.area.BottomRight.X - playerSize.X, playerYStart), new Vector2(0, 0.5f));
            base.SharedInitialised(state);
        }

        public override void Initialised()
        {
            this.ball?.StartMoving();
            base.Initialised();
        }

        public override void Render(Surface surface, GameTime time)
        {
            surface.Clear();

            base.Render(surface, time);
        }
    }
}