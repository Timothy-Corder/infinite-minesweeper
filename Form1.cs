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
using static System.Collections.Specialized.BitVector32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

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

        readonly Bitmap[] numbers = new Bitmap[8] { Assets.one, Assets.two, Assets.three, Assets.four, Assets.five, Assets.six, Assets.seven, Assets.eight };
        readonly Bitmap flag = Assets.flag;
        readonly Bitmap mine = Assets.mine;
        readonly Bitmap boom = Assets.boom;

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
            if (viewportVelocity.Length() > 1)
                viewportVelocity = viewportVelocity.Normalize();
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
                }
            }
            Invalidate();
        }

        internal void GetSave()
        {
            seed = new byte[4];
            Save.Read(seed, 0, 4);

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
                        failedSectors.Add(new Vector2(x, y));
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
        }
        internal bool OpenCell(Vector2 cell)
        {
            Vector2 sector = new Vector2(((int)cell.X) / 10,((int)cell.Y) / 10);

            if (failedSectors.Contains(sector) || succeededSectors.Contains(sector))
            {
                return true;
            }

            if (cell == null || openCells.Contains(cell))
            {
                return true;
            }
            if (CheckIsMine((int)cell.X, (int)cell.Y, MineChance))
            {
                return false;
            }
            else if (!(openCells.Contains(cell) || flaggedCells.Contains(cell)))
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
            bool sectorCleared = false;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (!openCells.Contains(new Vector2((int)sector.X * 10 + i, (int)sector.Y * 10 + j)))
                    {
                        if (!(flaggedCells.Contains(new Vector2((int)sector.X * 10 + i, (int)sector.Y * 10 + j)) && CheckIsMine((int)sector.X * 10 + i, (int)sector.Y * 10 + j, MineChance)))
                        sectorCleared = false;
                        break;
                    }
                    sectorCleared = true;
                }
                if (!sectorCleared)
                {
                    break;
                }
            }
            if (sectorCleared)
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        openCells.Remove(new Vector2((int)sector.X * 10 + i, (int)sector.Y * 10 + j));
                    }
                    if (!sectorCleared)
                    {
                        break;
                    }
                }
                succeededSectors.Add(sector);
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
            int horizontalLines = (int)Math.Ceiling((float)ClientSize.Height / ViewportZoom) + 1;
            int verticalLines = (int)Math.Ceiling((float)ClientSize.Width / ViewportZoom) + 1;

            // Reveal everything in a failed sector, draw a mine on top of all mines, draw a flag on top of all flagged cells, and darken the whole sector
            
            // Draw a flag on top of all flagged cells
            foreach (Vector2 cell in flaggedCells)
            {
                g.DrawImage(flag, new RectangleF(((cell - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
            }

            // Write each cell's neighbor count, unless it's zero
            foreach (Vector2 cell in openCells)
            {
                g.FillRectangle(Brushes.White, (cell.X - viewportLocation.X) * ViewportZoom, (cell.Y - viewportLocation.Y) * ViewportZoom, ViewportZoom, ViewportZoom);

                int count = NeighborCount((int)cell.X, (int)cell.Y);

                if (count > 0 && ViewportZoom > 8)
                {
                    g.DrawImage(numbers[count - 1], new RectangleF(((cell - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
                }
            }

            foreach (Vector2 sector in failedSectors)
            {
                // Skip sectors that are outside the viewport
                if (sector.X < viewportLocation.X || sector.X > viewportLocation.X + ClientSize.Width / ViewportZoom || sector.Y < viewportLocation.Y || sector.Y > viewportLocation.Y + ClientSize.Height / ViewportZoom)
                {
                    continue;
                }

                g.FillRectangle(Brushes.White, ((sector.X * 10) - viewportLocation.X) * ViewportZoom, ((sector.Y * 10) - viewportLocation.Y) * ViewportZoom, ViewportZoom * 10, ViewportZoom * 10);

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        int x = (int)sector.X * 10 + i;
                        int y = (int)sector.Y * 10 + j;
                        if (CheckIsMine(x, y, MineChance))
                        {
                            if (!booms.Contains(new Vector2(x, y)))
                            {
                                g.DrawImage(mine, new RectangleF(((new Vector2(x, y) - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
                            }
                            else
                            {

                                g.DrawImage(boom, new RectangleF(((new Vector2(x, y) - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
                            }
                        }
                        else
                        {
                            int count = NeighborCount(x, y);
                            if (count > 0 && ViewportZoom > 8)
                            {
                                g.DrawImage(numbers[count - 1], new RectangleF(((new Vector2(x, y) - viewportLocation) * ViewportZoom) + new Vector2((float)ViewportZoom / 10), new Vector2((float)ViewportZoom * 0.8f)));
                            }
                        }
                    }
                }

                g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.Black)), ((sector.X * 10) - viewportLocation.X) * ViewportZoom, ((sector.Y * 10) - viewportLocation.Y) * ViewportZoom, ViewportZoom * 10, ViewportZoom * 10);
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
}
