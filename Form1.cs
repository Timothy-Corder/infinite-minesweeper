using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TimUtils;

namespace InfiniteMineSweeper
{
    public partial class GameForm : Form
    {
        FileStream Save;
        byte[] seed;
        Vector2 viewportLocation = Vector2.Zero;
        Vector2 viewportVelocity = Vector2.Zero;
        List<Vector2> openCells = new List<Vector2>();
        List<Vector2> flaggedCells = new List<Vector2>();
        List<Vector2> booms = new List<Vector2>();
        List<Vector2> failedSectors = new List<Vector2>();
        List<Vector2> succeededSectors = new List<Vector2>();
        private HashSet<Keys> pressedKeys = new HashSet<Keys>();
        int zoom;
        int ViewportZoom
        {
            get { return zoom; }
            set { zoom = Clamp(value, 5, 64); }
        }
        int Seed
        {
            get
            {
                return BitConverter.ToInt32(seed, 0);
            }
        }
        int MineChance = 20;

        Pen thinPen = new Pen(Color.Black, 1);
        Pen thickPen = new Pen(Color.Black, 3);

        readonly Bitmap[] numbers = new Bitmap[9] { Assets.zero, Assets.one, Assets.two, Assets.three, Assets.four, Assets.five, Assets.six, Assets.seven, Assets.eight };
        readonly Bitmap flag = Assets.flag;
        readonly Bitmap mine = Assets.mine;
        readonly Bitmap boom = Assets.boom;

        private RectangleF drawingRect = new RectangleF();

        public GameForm(FileStream save, int score, int fails)
        {
            Disposed += (sender, e) =>
            {
                if (Save != null && Save.CanWrite)
                {
                    SaveGame();
                    save.Close();
                }
            };
            Save = save;
            DoubleBuffered = true;
            WindowState = FormWindowState.Maximized;
            InitializeGame();

            InitializeComponent();

            Timer timer = new Timer();
            timer.Interval = 16;
            timer.Tick += (sender, e) =>
            {
                UpdateViewportMovement();
            };
            timer.Start();
        }
        internal void InitializeGame()
        {
            GetSave();
            ViewportZoom = 100;

            MouseWheel += GameForm_Scroll;
            MouseClick += OpenClickedCell;
            KeyDown += GameForm_KeyDown;
            KeyUp += GameForm_KeyUp;
        }
        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode);
        }

        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            pressedKeys.Remove(e.KeyCode);
        }

        private void UpdateViewportMovement()
        {
            viewportVelocity = Vector2.Zero; // Reset before recalculating

            if (pressedKeys.Contains(Keys.W)) viewportVelocity.Y -= 1;
            if (pressedKeys.Contains(Keys.S)) viewportVelocity.Y += 1;
            if (pressedKeys.Contains(Keys.A)) viewportVelocity.X -= 1;
            if (pressedKeys.Contains(Keys.D)) viewportVelocity.X += 1;

            // Normalize movement to prevent diagonal speed increase
            MoveViewport(viewportVelocity);
        }

        private void GameForm_Scroll(object sender, MouseEventArgs e)
        {
            float zoomChange = 0;
            if (e.Delta > 0)
            {
                zoomChange = (float)ViewportZoom * 1.2f;
            }
            else
            {
                zoomChange = (float)ViewportZoom / 1.2f;
            }
            ViewportZoom = (int)zoomChange;
            Invalidate();
        }

        private void OpenClickedCell(object sender, MouseEventArgs e)
        {
            Vector2 cell = new Vector2(
    (int)Math.Floor((e.X / (float)ViewportZoom) + viewportLocation.X),
    (int)Math.Floor((e.Y / (float)ViewportZoom) + viewportLocation.Y)
);
            if (e.Button == MouseButtons.Left)
            {
                if (!OpenCell(cell))
                {
                    booms.Add(cell);
                    failedSectors.Add(new Vector2((int)cell.X / 10, (int)cell.Y / 10));
                    Vector2 sector = new Vector2(((int)cell.X) / 10, ((int)cell.Y) / 10);
                    for (int i = 0; i < 10; i++)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            openCells.Remove(new Vector2((int)sector.X * 10 + i, (int)sector.Y * 10 + j));
                        }
                    }
                    //// I'll miss this code. Deleting the save was funny.
                    //MessageBox.Show("You lose!");
                    //// Delete the save file lol
                    //Save.Close();
                    //File.Delete("InfMS.save");
                    //Environment.Exit(0);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (flaggedCells.Contains(cell))
                {
                    flaggedCells.Remove(cell);
                }
                else
                {
                    flaggedCells.Add(cell);
                    if (CheckSuccess(cell / 10))
                    {
                        AddToSuccess(cell / 10);
                    }
                }
            }
            Invalidate();
        }

        void AddToSuccess(Vector2 sector)
        {
            sector.X = (int)sector.X;
            sector.Y = (int)sector.Y;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector2 cell = new Vector2(sector.X * 10 + i, sector.Y * 10 + j);
                    openCells.Remove(cell);
                    flaggedCells.Remove(cell);
                }
            }
            succeededSectors.Add(sector);
        }

        internal void GetSave()
        {
            seed = new byte[4];
            Save.Read(seed, 0, 4);
            
            // Skip over the scores
            {
                byte[] trashcan = new byte[8];
                Save.Read(trashcan, 0, 8);
            }

            // Read the rest of the file as open cells, flags, or failed sectors, saved as x y pairs and a type byte. Any cell not in the file is considered closed.
            while (Save.Position < Save.Length)
            {
                byte[] buffer = new byte[9];
                Save.Read(buffer, 0, 9);
                int x = BitConverter.ToInt32(buffer, 0);
                int y = BitConverter.ToInt32(buffer, 4);
                byte type = buffer[8];

                switch (type)
                {
                    case 0x00:
                        openCells.Add(new Vector2(x, y));
                        break;
                    case 0x01:
                        flaggedCells.Add(new Vector2(x, y));
                        break;
                    case 0x02:
                        booms.Add(new Vector2(x, y));
                        failedSectors.Add(new Vector2(x / 10, y / 10));
                        break;
                    case 0x03:
                        succeededSectors.Add(new Vector2(x, y));
                        break;
                }
                if (type == 0x01)
                {
                    flaggedCells.Add(new Vector2(x, y));
                }
            }
        }
        internal void SaveGame()
        {
            Save.Seek(0, SeekOrigin.Begin);
            Save.Write(seed, 0, 4);

            Save.Write(BitConverter.GetBytes(succeededSectors.Count),0, 4);
            Save.Write(BitConverter.GetBytes(failedSectors.Count), 0, 4);

            foreach (Vector2 cell in openCells)
            {
                byte[] buffer = new byte[9];
                byte type = 0x00;
                BitConverter.GetBytes((Int32)cell.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)cell.Y).CopyTo(buffer, 4);
                (new byte[1] { type }).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
            foreach (Vector2 cell in flaggedCells)
            {
                byte[] buffer = new byte[9];
                byte type = 0x01;
                BitConverter.GetBytes((Int32)cell.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)cell.Y).CopyTo(buffer, 4);
                (new byte[1] { type }).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
            foreach (Vector2 cell in booms)
            {
                byte[] buffer = new byte[9];
                byte type = 0x02;
                BitConverter.GetBytes((Int32)cell.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)cell.Y).CopyTo(buffer, 4);
                (new byte[1] { type }).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
            foreach (Vector2 sector in succeededSectors)
            {
                byte[] buffer = new byte[9];
                byte type = 0x03;
                BitConverter.GetBytes((Int32)sector.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)sector.Y).CopyTo(buffer, 4);
                (new byte[1] { type }).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
        }
        internal bool OpenCell(Vector2 cell)
        {
            // Calculate sector - handle negative coordinates properly
            int sectorX = (int)Math.Floor(cell.X / 10.0f);
            int sectorY = (int)Math.Floor(cell.Y / 10.0f);
            Vector2 sector = new Vector2(sectorX, sectorY);

            int state = 0;
            if (openCells.Contains(cell)) state = 1;
            if (flaggedCells.Contains(cell)) state = 2;
            if (failedSectors.Contains(sector) || succeededSectors.Contains(sector)) state = 3;

            // If the cell has already been in some way processed
            if (state != 0)
            {
                return true;
            }

            // Check if it's a mine
            if (CheckIsMine((int)cell.X, (int)cell.Y, MineChance))
            {
                return false;
            }

            // Process cell if not already flagged or open
            if (!(state == 1 || state == 2))
            {
                openCells.Add(cell);

                // If zero neighbors, recursively open adjacent cells
                if (NeighborCount((int)cell.X, (int)cell.Y) == 0)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            OpenCell(cell + new Vector2(i, j));
                        }
                    }
                }
            }

            // Check if sector is cleared
            if (CheckSuccess(sector))
            {
                AddToSuccess(sector);
            }

            return true;
        }
        public bool CheckIsMine(int x, int y, int percentChance)
        {
            // Combine seed, x, and y into a single deterministic hash
            int hash = HashPosition(Seed, x, y);

            // Use the hash to create a random number
            Random random = new Random(hash);

            // Generate a random boolean
            return random.Next(100) <= percentChance;
        }
        public int NeighborCount(int x, int y)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }
                    if (CheckIsMine(x + i, y + j, MineChance))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        private int HashPosition(int seed, int x, int y)
        {
            unchecked
            {
                int hash = seed ^ (x * 73856093) ^ (y * 19349663);
                return Math.Abs(hash) % 23389859;
            }
        }

        bool CheckSuccess(Vector2 sector)
        {
            // Check if all cells in the sector are either open or flagged
            sector.X = (int)sector.X;
            sector.Y = (int)sector.Y;

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    Vector2 cell = new Vector2(sector.X * 10 + i, sector.Y * 10 + j);
                    if (!openCells.Contains(cell) && !flaggedCells.Contains(cell))
                    {
                        return false;
                    }
                    if (flaggedCells.Contains(cell) && !CheckIsMine((int)cell.X, (int)cell.Y, MineChance))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        void MoveViewport(Vector2 direction)
        {
            float speedFactor = 1f + (50f / ViewportZoom); // Adjust 50f for fine-tuning
            viewportLocation += direction * speedFactor;
            Invalidate();
        }

        int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private void DrawCell(Graphics g, Vector2 cell, Image image)
        {
            drawingRect.X = ((cell.X - viewportLocation.X) * ViewportZoom) + (ViewportZoom / 10);
            drawingRect.Y = ((cell.Y - viewportLocation.Y) * ViewportZoom) + (ViewportZoom / 10);
            drawingRect.Width = ViewportZoom * 0.8f;
            drawingRect.Height = ViewportZoom * 0.8f;
            g.DrawImage(image, drawingRect);
        }

        private bool IsInViewport(Vector2 position)
        {
            return position.X >= viewportLocation.X - 1 &&
                   position.X <= viewportLocation.X + ClientSize.Width / ViewportZoom + 1 &&
                   position.Y >= viewportLocation.Y - 1 &&
                   position.Y <= viewportLocation.Y + ClientSize.Height / ViewportZoom + 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.Clear(Color.LightGray);

            // Draw the grid lines

            // Calculate the number of horizontal and vertical lines to draw. The lines are drawn independently of the location and zoom, so we need to draw enough lines to cover the entire screen, as closely as they should be drawn.
            int horizontalLines = (int)Math.Ceiling((float)ClientSize.Height / ViewportZoom) + 1;
            int verticalLines = (int)Math.Ceiling((float)ClientSize.Width / ViewportZoom) + 1;

            // Reveal everything in a failed sector, draw a mine on top of all mines, draw a flag on top of all flagged cells, and darken the whole sector

            // Draw a flag on top of all flagged cells
            foreach (Vector2 cell in flaggedCells)
            {
                if (IsInViewport(cell))
                {
                    DrawCell(g, cell, flag);
                }
            }

            // Write each cell's neighbor count, unless it's zero
            foreach (Vector2 cell in openCells)
            {

                int count = NeighborCount((int)cell.X, (int)cell.Y);

                if (ViewportZoom > 8)
                {

                    if (IsInViewport(cell))
                    {
                        DrawCell(g, cell, numbers[count]);
                    }
                }
                else
                {
                    g.FillRectangle(Brushes.White, new RectangleF(((cell - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
                }
            }

            foreach (Vector2 sector in failedSectors)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        int x = (((int)sector.X) * 10) + i;
                        int y = (((int)sector.Y) * 10) + j;
                        Vector2 cell = new Vector2(x, y);
                        if (CheckIsMine(x, y, MineChance))
                        {
                            if (!booms.Contains(new Vector2(x, y)))
                            {
                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, mine);
                                }
                            }
                            else
                            {
                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, boom);
                                }
                            }
                        }
                        else
                        {
                            int count = NeighborCount(x, y);
                            if (ViewportZoom > 8)
                            {

                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, numbers[count]);
                                }
                            }
                            else
                            {
                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, numbers[count]);
                                }
                            }
                        }
                    }
                }

                g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.Red)), ((sector.X * 10) - viewportLocation.X) * ViewportZoom, ((sector.Y * 10) - viewportLocation.Y) * ViewportZoom, ViewportZoom * 10, ViewportZoom * 10);
            }
            foreach (Vector2 sector in succeededSectors)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        int x = (((int)sector.X) * 10) + i;
                        int y = (((int)sector.Y) * 10) + j;
                        Vector2 cell = new Vector2(x, y);
                        if (CheckIsMine(x, y, MineChance))
                        {
                            if (IsInViewport(cell))
                            {
                                DrawCell(g, cell, flag);
                            }
                        }
                        else
                        {
                            int count = NeighborCount(x, y);
                            if (ViewportZoom > 8)
                            {

                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, numbers[count]);
                                }
                            }
                            else
                            {
                                if (IsInViewport(cell))
                                {
                                    DrawCell(g, cell, numbers[count]);
                                }
                            }
                        }
                    }
                }

                g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.LightCyan)), ((sector.X * 10) - viewportLocation.X) * ViewportZoom, ((sector.Y * 10) - viewportLocation.Y) * ViewportZoom, ViewportZoom * 10, ViewportZoom * 10);
            }

            // Draw the lines over, making some thicker to group the grid into 10x10 sectors
            for (int i = 0; i < horizontalLines; i++)
            {
                float y = (i - (viewportLocation.Y % 1)) * ViewportZoom;
                Pen pen = ((i + (int)viewportLocation.Y) % 10 == 0) ? thickPen : thinPen;
                g.DrawLine(pen, 0, y, ClientSize.Width, y);
            }
            for (int i = 0; i < verticalLines; i++)
            {
                float x = (i - (viewportLocation.X % 1)) * ViewportZoom;
                Pen pen = ((i + (int)viewportLocation.X) % 10 == 0) ? thickPen : thinPen;
                g.DrawLine(pen, x, 0, x, ClientSize.Height);
            }
        }
    }
    public partial class MenuForm : Form
    {
        Vector2 backgroundPosition = new Vector2(Vector2.Zero);
        int backgroundSeed = new Random().Next();
        public int save = 0;
        public MenuForm()
        {
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;

            ClientSize = new Vector2(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;

            Timer timer = new Timer();
            timer.Interval = 16;
            timer.Tick += (s, e) => { Invalidate(); };
            timer.Start();

            for (int i = 1; i <= 4; i++)
            {
                Button btn = new Button();
                btn.Text = $"Save {i}";
                btn.Tag = i;
                btn.Size = new Size(200, 50);
                btn.Location = new Point(400, 250 + (i - 1) * 60);
                btn.Click += (s, e) => { save = (int)((Button)s).Tag; Close(); };
                Controls.Add(btn);

                // Check if the save exists so we know if we should add a delete button
                if (File.Exists($"save{i}.imssave"))
                {
                    Button deleteBtn = new Button();
                    deleteBtn.Text = $"Delete Save {i}";
                    deleteBtn.Tag = i;
                    deleteBtn.Size = new Size(200, 50);
                    deleteBtn.Location = new Point(600, 250 + (i - 1) * 60);
                    deleteBtn.Click += (s, e) =>
                    {
                        File.Delete($"save{(int)((Button)s).Tag}.imssave");
                        Controls.Remove(deleteBtn);
                    };
                    Controls.Add(deleteBtn);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(Color.LightGray);

            // Draw the background, a grid of lines moving down and to the right
            int lineDistance = 50;
            float speed = 0.5f;

            backgroundPosition += new Vector2(speed);

            Bitmap background = new Bitmap(ClientSize.Width, ClientSize.Height);
            Graphics bg = Graphics.FromImage(background);
            bg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            // Randomly replace some grid spots with mines, keeping them consistent
            for (int x = 0; x < (ClientSize.Width / lineDistance) + 1; x++)
            {
                for (int y = 0; y < (ClientSize.Height / lineDistance) + 1; y++)
                {
                    Random rand;

                    rand = new Random(unchecked(backgroundSeed * (x * 1618033 ^ y * 4514113)));
                    if (rand.Next(100) <= 5)
                    {
                        bg.FillRectangle(Brushes.White, ((backgroundPosition.X + x * lineDistance) % (ClientSize.Width + lineDistance)) - lineDistance, ((backgroundPosition.Y + y * lineDistance) % (ClientSize.Height + lineDistance)) - lineDistance, lineDistance, lineDistance);
                        bg.DrawImage(Assets.mine, ((backgroundPosition.X + x * lineDistance) % (ClientSize.Width + lineDistance)) - lineDistance, ((backgroundPosition.Y + y * lineDistance) % (ClientSize.Height + lineDistance)) - lineDistance, lineDistance, lineDistance);
                    }
                }
            }

            for (int x = 0; x < ClientSize.Width; x += lineDistance)
            {
                bg.DrawLine(Pens.Black, x + backgroundPosition.X % lineDistance, 0, x + backgroundPosition.X % lineDistance, ClientSize.Height);
            }
            for (int y = 0; y < ClientSize.Height; y += lineDistance)
            {
                bg.DrawLine(Pens.Black, 0, y + backgroundPosition.Y % lineDistance, ClientSize.Width, y + backgroundPosition.Y % lineDistance);
            }

            bg.FillRectangle(new SolidBrush(Color.FromArgb(200, Color.White)), ClientRectangle);

            bg.Save();

            g.DrawImage(background, 0, 0);


            g.DrawString("Infinite Mine Sweeper", new Font("Arial", 24), Brushes.Black, new PointF(10, 10));
            g.DrawString("Click anywhere to start", new Font("Arial", 12), Brushes.Black, new PointF(10, 40));
        }
    }
}
