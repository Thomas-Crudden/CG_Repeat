using GameClassLibrary;
using Microsoft.AspNet.SignalR;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using System.Threading.Tasks;

namespace GameServer
{
    public class GameHub : Hub
    {

        static string playerOneChar;
        //static string playerOne;
        //static string playerTwo;
        static int c = 0;
        Random r = new Random();
        static List<Vector2> positionsCollectables = new List<Vector2>();
        static Timer t;

        static bool gameRunning = false;


        #region StartGame send once

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public void SendPlayer(string charType)
        {
            if (playerOneChar == null)
            {
                SendCollectables();
                playerOneChar = charType;
            }
            else
            {
                SendCollectables();
                Clients.Caller.otherStartpoint(new Vector2(700, 200));
                Clients.Caller.sendPlayer(/*playerOne*/ playerOneChar);
                Clients.Others.sendPlayer(Context.ConnectionId, charType);
                gameRunning = true;
                GameStart(gameRunning);
                playerOneChar = null;
            }
        }

        
        public void SendCollectables()
        {
            if (positionsCollectables.Count == 0 && c == 0)
            {
                c++;
                int temp = r.Next(3, 10);
                for (int i = 0; i < temp; i++) //create collectables
                {
                    positionsCollectables.Add(new Vector2(r.Next(50, 700), r.Next(50, 500)));
                }

            }
            else
            {
                while (c != 1)
                { }
                c = 0;
                Clients.All.sendPositionCollectables(positionsCollectables);

                positionsCollectables.Clear();
            }

        }

        #endregion

        #region Update

        public void UpdatePosition(Vector2 newPlayerPositon)
        {
            Clients.Others.updatePosition(newPlayerPositon);
        }

        #endregion

        #region Trigger Methodes

        public void NewProjectile(Vector2 startPosition, Vector2 flyDirection)
        {
            Clients.Others.newProjectile(Context.ConnectionId, startPosition, flyDirection);
        }

        public void GameStart(bool g)
        {
            gameRunning = g;
        }

        #endregion
    }
}