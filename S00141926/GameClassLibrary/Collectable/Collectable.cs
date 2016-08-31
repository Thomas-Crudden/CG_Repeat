﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClassLibrary
{
    public class Collectable : Sprite
    {
        public int Value;

        public Collectable(Texture2D tex, Vector2 pos) : base(tex, pos)
        {
            Value = 10;

        }
    }
}
