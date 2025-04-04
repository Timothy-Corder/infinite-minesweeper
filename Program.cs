using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InfiniteMineSweeper
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int wins1 = -1;
            int losses1 = -1;
            int wins2 = -1;
            int losses2 = -1;
            int wins3 = -1;
            int losses3 = -1;
            int wins4 = -1;
            int losses4 = -1;


            try
            {
                FileStream s = File.Open("save1.imssave", FileMode.Open);

                byte[] Ws = new byte[4];
                s.Read(Ws, 4, 4);
                byte[] Ls = new byte[4];
                s.Read(Ls, 0, 4);

                int win = BitConverter.ToInt32(wins, 0);
                int loss = BitConverter.ToInt32(losses, 0);
            }

            MenuForm menu = new MenuForm();
            Application.Run(menu);
            string savePath = $"save{ menu.save }.imssave";

            FileStream save;

            if (!File.Exists(savePath))
            {
                save = File.Create(savePath);
                Random random = new Random();
                byte[] seed = BitConverter.GetBytes(random.Next());
                save.Write(seed, 0, 4);
                save.WriteByte(0);
                save.WriteByte(0);
                save.Close();
            }

            save = File.Open(savePath, FileMode.Open);

            byte[] wins = new byte[4];
            save.Read(wins, 4, 4);
            byte[] losses = new byte[4];
            save.Read(losses, 0, 4);

            int win = BitConverter.ToInt32(wins, 0);
            int loss = BitConverter.ToInt32(losses, 0);

            Application.Run(new GameForm(save));
        }
    }
}
