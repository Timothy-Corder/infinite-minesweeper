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

            int[] wins = new int[4];
            int[] losses = new int[4];


            for (int i = 0; i < 4; i++)
            {
                FileStream s = null;
                try
                {
                    s = File.Open($"save{i+1}.imssave", FileMode.Open);
                    s.Seek(4, SeekOrigin.Begin);

                    byte[] Ws = new byte[4];
                    s.Read(Ws, 0, 4);
                    byte[] Ls = new byte[4];
                    s.Read(Ls, 0, 4);

                    wins[i] = BitConverter.ToInt32(Ws, 0);
                    losses[i] = BitConverter.ToInt32(Ls, 0);
                    s.Dispose();
                }
                catch
                {
                    if (s != null)
                    {
                        s.Dispose();
                    }
                }
            }
            
            MenuForm menu = new MenuForm(wins, losses);
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
                save.Dispose();
            }

            save = File.Open(savePath, FileMode.Open);
            var test = new byte[12];
            save.Read(test, 0, 12);
            foreach (byte b in test)
            {
                Console.Write(b + " ");
            }
            save.Seek(0, SeekOrigin.Begin);


            Application.Run(new GameForm(save));
        }
    }
}
