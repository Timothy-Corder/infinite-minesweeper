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

            MenuForm menu = new MenuForm();
            Application.Run(menu);
            string savePath = menu.SavePath;

            FileStream save = null;
            if (!File.Exists("InfMS.save"))
            {
                save = File.Create("InfMS.save");
                Random random = new Random();
                byte[] seed = BitConverter.GetBytes(random.Next());
                save.Write(seed, 0, 4);
                save.Close();
            }

            save = File.Open("InfMS.save", FileMode.Open);

            Application.Run(new GameForm(save));
        }
    }
}
