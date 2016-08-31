using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using GameClassLibrary;
using System.Collections.Generic;
using System;
using Microsoft.AspNet.SignalR.Client;
using WebAPIAuthenticationClient;
using AchievevementWebAPIExample;

namespace Game1
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Player player;
        Player Enemy;
        string clientID;
        Color playerColor = Color.Green;
        Color enemyColor = Color.Red;
        float timer;
        int timecounter;

        Texture2D backgroundTexture;
        Texture2D[] textures;
        Texture2D textureCollectable;
        Texture2D texHealth;
        SpriteFont message;
        string Message;

        public List<Projectile> Projectiles = new List<Projectile>();
        List<Collectable> Collectables = new List<Collectable>();
        List<Collectable> pickUp = new List<Collectable>();
        List<Projectile> destroyProjectiles = new List<Projectile>();
        Projectile newProjectile;

        static Random r = new Random();
        bool gameStarted = false;

        enum currentDisplay { Selection, Game, Score };
        currentDisplay currentState = currentDisplay.Selection;

        enum endGameStatuses { Win, Lose, Draw }
        endGameStatuses gameOutcome = endGameStatuses.Draw;

        Menu menu;
        string[] menuOptions = new string[] { "Ship1", "Ship2", "Ship3" };
        Vector2 startVector = new Vector2(50, 250);

        KeyboardState oldState;
        KeyboardState newState;

        static IHubProxy proxy;
        HubConnection connection = new HubConnection("http://localhost:5553/");


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            oldState = Keyboard.GetState();
            graphics.PreferredBackBufferWidth = 800; 
            graphics.PreferredBackBufferHeight = 600;
            graphics.ApplyChanges();

            try
            {
                bool valid = PlayerAuthentication.login("powell.paul@itsligo.ie", "itsPaul$1").Result;

                if (valid) Message = "Player Logged in with Token " + PlayerAuthentication.PlayerToken;
                else Message = PlayerAuthentication.PlayerToken;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }


            proxy = connection.CreateHubProxy("GameHub");
            clientID = connection.ConnectionId;

            Action<string, string> ReceivePlayer = receivePlayerMessage;
            proxy.On("sendPlayer", ReceivePlayer);

            Action<Vector2> ReceiveNewPosition = receiveNewPlayerPosition;
            proxy.On("updatePosition", ReceiveNewPosition);

            Action<List<Vector2>> ReceiveCollectablePositions = receiveCollectablePositions;
            proxy.On("sendPositionCollectables", ReceiveCollectablePositions);

            Action<string, Vector2, Vector2> ReceiveNewProjectile = receiveNewEnemyProjectile;
            proxy.On("newProjectile", ReceiveNewProjectile);

            Action<Vector2> ReceiveDiffrentStartposition = receiveDiffrentStartposition;
            proxy.On("otherStartpoint", ReceiveDiffrentStartposition);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            IsMouseVisible = true;
            spriteBatch = new SpriteBatch(GraphicsDevice);

            message = Content.Load<SpriteFont>("SpriteFont\\message"); 

            #region Loading Textures

            backgroundTexture = Content.Load<Texture2D>("Background\\BG1"); 

            textures = new Texture2D[] 
            {
                Content.Load<Texture2D>("Sprites\\tcShip1"),
                Content.Load<Texture2D>("Sprites\\tcShip2"),
                Content.Load<Texture2D>("Sprites\\tcShip3")
            };

          
            textureCollectable = Content.Load<Texture2D>("Sprites\\star");
            texHealth = Content.Load<Texture2D>("Sprites\\healthBar");

            #endregion

            Console.WriteLine("Connecting");
            connection.Start().Wait();
            Console.WriteLine("Connected");

        }

        protected override void Update(GameTime gameTime)
        {
            newState = Keyboard.GetState(); 



            #region Choose  Characters

            if (currentState == currentDisplay.Selection)
            {
                menu.CheckMouse();

                player = createPlayer(clientID, menu.MenuAction, playerColor);

                if (player != null)
                {
                    proxy.Invoke("SendPlayer", menu.MenuAction); 
                }

                menu.MenuAction = null; 
            }

            #endregion


            #region Logic

            if (currentState == currentDisplay.Game) 
            {
                if (gameStarted)
                {
                    player.Move(newState); 
                    proxy.Invoke("UpdatePosition", player._position);

                    timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    timecounter += (int)timer;
                    if (timer >= 1.0F) timer = 0F;

                    #region Collision Logic
                    foreach (var item in Projectiles) 
                    {
                      
                        if (item.CollisiionDetection(Enemy.Rectangle))
                            Enemy.PlayerChar.GotShoot(item);

                        if (item.CollisiionDetection(player.Rectangle))
                            player.PlayerChar.GotShoot(item);
                    }

                    foreach (var item in Collectables)
                    {
                        if (player.CollisiionDetection(item.Rectangle))
                        {
                            pickUp.Add(item);
                            item.IsVisible = false;
                            player.Collect(item);
                        }

                        if (Enemy.CollisiionDetection(item.Rectangle))
                        {
                            pickUp.Add(item);
                            item.IsVisible = false;
                            Enemy.Collect(item);
                        }
                    }

                    #endregion

                    if (newState.IsKeyDown(Keys.Space) && oldState != newState && gameStarted)
                    {
                        newProjectile = player.PlayerChar.Shoot(player._position, player.FireDirection, playerColor); 
                        if (newProjectile != null)
                        {
                            Projectiles.Add(newProjectile); 
                            proxy.Invoke("NewProjectile", newProjectile._position, newProjectile.flyDirection);
                        }

                    }
                    

                    foreach (var item in Projectiles)
                    {
                        item.Update();
                        if (OutsideScreen(item))
                        {
                            destroyProjectiles.Add(item);
                        }
                    }

                    foreach (var item in pickUp)
                    {
                        Collectables.Remove(item);
                    }
                    foreach (var item in destroyProjectiles)
                    {
                        Projectiles.Remove(item);
                    }

                 
                    pickUp.Clear();
                    destroyProjectiles.Clear();

                    if (timecounter == 180)
                    {
                        currentState = currentDisplay.Score;
                    }

                    if (Collectables.Count == 0)
                    {
                        currentState = currentDisplay.Score;
                    }


                    if (Enemy.PlayerChar.Health <= 0)
                    {
                        currentState = currentDisplay.Score;
                    }

                    if (player.PlayerChar.Health <= 0)
                    {
                        currentState = currentDisplay.Score;
                    }

                    if (currentState == currentDisplay.Score)
                    {
                        gameStarted = false;
                        proxy.Invoke("StartGame", gameStarted);
                        if (player.score > Enemy.score)
                            gameOutcome = endGameStatuses.Win;

                        if (player.score < Enemy.score)
                            gameOutcome = endGameStatuses.Lose;

                        if (player.score == Enemy.score)
                            gameOutcome = endGameStatuses.Draw;
                    }

                }
            }

            #endregion

            if (newState.IsKeyDown(Keys.Escape) && oldState != newState) 
                Exit();

            base.Update(gameTime);

            oldState = newState;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);

            if (currentState == currentDisplay.Selection)
                menu.Draw(spriteBatch); 

            #region Game 
            if (currentState == currentDisplay.Game) 
            {
                spriteBatch.Begin();
                spriteBatch.Draw(backgroundTexture, new Rectangle(0, 0, 800, 600), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f); //draw the background

                if (Enemy != null)
                    spriteBatch.DrawString(message, "Score: " + Enemy.score.ToString(), new Vector2(700, 0), enemyColor);
                spriteBatch.DrawString(message, "Score: " + player.score.ToString(), new Vector2(0, 0), playerColor);
                spriteBatch.End();

                if (Enemy != null)
                    Enemy.Draw(spriteBatch);

                if (timecounter >= 2) 
                {
                    spriteBatch.Begin();
                    spriteBatch.DrawString(message, "Collect the Stars", new Vector2(450, 25), Color.Black);
                    spriteBatch.End();
                }

                player.Draw(spriteBatch, message); 

                foreach (var item in Collectables)
                {
                    item.Draw(spriteBatch); 
                }

                foreach (var item in Projectiles)
                {
                    item.Draw(spriteBatch); 
                }

              
            }

            #endregion

            #region Score
       
            if (currentState == currentDisplay.Score)
            {
                player._position = new Vector2(300, 400);
                Enemy._position = new Vector2(450, 400);

                player.Draw(spriteBatch);
                Enemy.Draw(spriteBatch);
                Vector2 fontPos = new Vector2(player._texture.Width / 2, -10);
                Vector2 namePos = new Vector2(player._texture.Width / 2, player._texture.Height + 10);

                spriteBatch.Begin();
                spriteBatch.DrawString(message, gameOutcome.ToString(), new Vector2(350, 100), Color.BlueViolet, 0, Vector2.Zero, 3f, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, player.score.ToString(), fontPos + player._position, playerColor, 0, message.MeasureString(player.score.ToString()) / 2, 1, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, Enemy.score.ToString(), fontPos + Enemy._position, enemyColor, 0, message.MeasureString(Enemy.score.ToString()) / 2, 1, SpriteEffects.None, 0);

                spriteBatch.DrawString(message, "You", namePos + player._position, playerColor, 0, message.MeasureString("You") / 2, 1, SpriteEffects.None, 0);
                spriteBatch.DrawString(message, "Enemy", namePos + Enemy._position, enemyColor, 0, message.MeasureString("Enemy") / 2, 1, SpriteEffects.None, 0);
                spriteBatch.End();
            }

            #endregion

            base.Draw(gameTime);
        }

        #region Server Info



        private void receiveNewEnemyProjectile(string arg1, Vector2 arg2, Vector2 arg3)
        {
            Projectiles.Add(new Projectile(arg1, Enemy.PlayerChar._texture, Enemy.PlayerChar.strength, arg2, arg3, enemyColor));
        }

        private void receivePlayerMessage(string arg1, string arg2)
        {
            Enemy = createPlayer(arg1, arg2, enemyColor);
            gameStarted = true;

        }

        private void receiveNewPlayerPosition(Vector2 obj)
        {
            Enemy._position = obj;
        }

        private Player createPlayer(string id, string type, Color c)
        {
            Player temp = null;
            if (type != null)
            {

                switch (type.ToUpper()) 
                {
                    case "Ship1":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Ship(id, textures[0], 7, 3), texHealth, startVector, c, this);
                        break;

                    case "Ship2":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Ship(id, textures[1], 5, 4), texHealth, startVector, c, this);
                        break;

                    case "Ship3":
                        currentState = currentDisplay.Game;
                        temp = new Player(new Ship(id, textures[2], 3, 5), texHealth, startVector, c, this);
                        break;
                    default:
                        break;
                }
            }


            return temp;
        }

        private void receiveCollectablePositions(List<Vector2> obj)
        {
            foreach (var item in obj)
            {
                Collectables.Add(new Collectable(textureCollectable, item));
            }
        }

        private void receiveDiffrentStartposition(Vector2 obj)
        {
            player._position = obj;
        }

        public bool OutsideScreen(Sprite obj)
        {
            if (!obj.Rectangle.Intersects(Window.ClientBounds))
            {
                return true;
            }
            else
                return false;
        }

      
        
        #endregion
    }
}
