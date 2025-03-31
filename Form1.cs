using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        Pen thinPen = new Pen(Color.Black, 3);
        Pen thickPen = new Pen(Color.Black, 5);
        public GameForm(FileStream save)
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
            if (viewportVelocity.Length() > 1)
                viewportVelocity = viewportVelocity.Normalize();
            MoveViewport(viewportVelocity);
        }

        private void GameForm_Scroll(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                ViewportZoom = (int)((float)ViewportZoom * 1.2f);
            }
            else
            {
                ViewportZoom = (int)((float)ViewportZoom / 1.2f);
            }
                Console.WriteLine(ViewportZoom);
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
                    MessageBox.Show("You lose!");
                    // Delete the save file lol
                    Save.Close();
                    File.Delete("InfMS.save");
                    Environment.Exit(0);
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
                }
            }
            Invalidate();
        }

        internal void GetSave()
        {
            seed = new byte[4];
            Save.Read(seed, 0, 4);

            // Read the rest of the file as open cells or flags, saved as x y pairs and a boolean. Any cell not in the file is considered closed.
            while (Save.Position < Save.Length)
            {
                byte[] buffer = new byte[9];
                Save.Read(buffer, 0, 9);
                int x = BitConverter.ToInt32(buffer, 0);
                int y = BitConverter.ToInt32(buffer, 4);
                bool flag = BitConverter.ToBoolean(buffer, 8);
                
                if (flag)
                {
                    flaggedCells.Add(new Vector2(x, y));
                }
                else
                {
                    openCells.Add(new Vector2(x, y));
                }
            }
        }
        internal void SaveGame()
        {
            Save.Seek(0, SeekOrigin.Begin);
            Save.Write(seed, 0, 4);
            foreach (Vector2 cell in openCells)
            {
                byte[] buffer = new byte[9];
                BitConverter.GetBytes((Int32)cell.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)cell.Y).CopyTo(buffer, 4);
                BitConverter.GetBytes(false).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
            foreach (Vector2 cell in flaggedCells)
            {
                byte[] buffer = new byte[9];
                BitConverter.GetBytes((Int32)cell.X).CopyTo(buffer, 0);
                BitConverter.GetBytes((Int32)cell.Y).CopyTo(buffer, 4);
                BitConverter.GetBytes(true).CopyTo(buffer, 8);
                Save.Write(buffer, 0, 9);
            }
        }
        internal bool OpenCell(Vector2 cell)
        {
            if (cell == null || openCells.Contains(cell))
            {
                return true;
            }
            if (CheckIsMine((int)cell.X, (int)cell.Y, MineChance))
            {
                return false;
            }
            else if (!openCells.Contains(cell))
            {
                openCells.Add(cell);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            g.Clear(Color.LightGray);

            // Draw the grid lines

            // Calculate the number of horizontal and vertical lines to draw. The lines are drawn independently of the location and zoom, so we need to draw enough lines to cover the entire screen, as closely as they should be drawn.
            int horizontalLines = (int)Math.Ceiling((float)ClientSize.Height / ViewportZoom);
            int verticalLines = (int)Math.Ceiling((float)ClientSize.Width / ViewportZoom);



            // Color all opened cells white, taking into account the viewport location and zoom
            foreach (Vector2 cell in openCells)
            {
                g.FillRectangle(Brushes.White, (cell.X - viewportLocation.X) * ViewportZoom, (cell.Y - viewportLocation.Y) * ViewportZoom, ViewportZoom, ViewportZoom);
            }
            // Draw a red square on top of all flagged cells
            foreach (Vector2 cell in flaggedCells)
            {
                g.FillRectangle(Brushes.Red, (cell.X - viewportLocation.X) * ViewportZoom, (cell.Y - viewportLocation.Y) * ViewportZoom, ViewportZoom, ViewportZoom);
            }

            Pen penToUse;
            if (ViewportZoom > 50)
            {
                penToUse = thickPen;
            }
            else if (ViewportZoom > 8)
            {
                penToUse = thinPen;
            }
            else
            {
                penToUse = Pens.Black;
            }

            // Draw sets of 10x10 lines to make the grid more visible
            for (int i = 0; i < horizontalLines; i += 10)
            {
                g.DrawLine(penToUse, 0, i * ViewportZoom - (viewportLocation.Y % 1) * ViewportZoom, ClientSize.Width, i * ViewportZoom - (viewportLocation.Y % 1) * ViewportZoom);
            }

            // Draw the lines over
            for (int i = 0; i < horizontalLines; i++)
            {
                g.DrawLine(penToUse, 0, i * ViewportZoom - (viewportLocation.Y % 1) * ViewportZoom, ClientSize.Width, i * ViewportZoom - (viewportLocation.Y % 1) * ViewportZoom);
            }
            for (int i = 0; i < verticalLines; i++)
            {
                g.DrawLine(penToUse, i * ViewportZoom - (viewportLocation.X % 1) * ViewportZoom, 0, i * ViewportZoom - (viewportLocation.X % 1) * ViewportZoom, ClientSize.Height);
            }

            // Write each cell's neighbor count, unless it's zero
            foreach (Vector2 cell in openCells)
            {
                int count = NeighborCount((int)cell.X, (int)cell.Y);
                if (count > 0)
                {
                    g.DrawString(count.ToString(), DefaultFont, Brushes.Black, (cell.X - viewportLocation.X) * ViewportZoom, (cell.Y - viewportLocation.Y) * ViewportZoom);
                }
            }
        }
    }
}
