using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Jindium
{
    class PHP
    {
        private static Random random = new Random();
        public static string RandomName(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //This is really just a quick demo with PHP. If you want to use PHP, you can use this.
        //But I will stress that you should not use this for anything serious.        
        public static byte[] ParseAndExecute(string phpPath, byte[] data)
        {
            Directory.CreateDirectory("PHP_TEMP");

            string randomNamePath = "PHP_TEMP/" + RandomName() + ".php";

            File.WriteAllBytes(randomNamePath, data);

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = $@"/c ""{phpPath}"" -f {randomNamePath}";
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            File.Delete(randomNamePath);

            return Encoding.UTF8.GetBytes(output);
        }
    }
}
