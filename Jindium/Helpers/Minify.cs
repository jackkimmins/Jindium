using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yahoo.Yui.Compressor;

namespace Jindium
{
    class Minify
    {
        public static string JavaScript(string content, bool enableOptimizations = false, bool obfuscate = false, System.Globalization.CultureInfo culture = null)
        {
            JavaScriptCompressor jsCom = new JavaScriptCompressor();
            jsCom.CompressionType = CompressionType.Standard;
            jsCom.ThreadCulture = culture ?? System.Globalization.CultureInfo.InvariantCulture;
            jsCom.Encoding = Encoding.UTF8;
            jsCom.DisableOptimizations = !enableOptimizations;
            jsCom.IgnoreEval = true;
            jsCom.LineBreakPosition = -1;
            jsCom.ObfuscateJavascript = obfuscate;
            jsCom.PreserveAllSemicolons = false;
            return jsCom.Compress(content);
        }

        public static string CSS(string content)
        {
            CssCompressor cssCom = new CssCompressor();
            cssCom.CompressionType = CompressionType.Standard;
            cssCom.LineBreakPosition = -1;
            cssCom.RemoveComments = true;
            return cssCom.Compress(content);
        }
    }
}
