using System.Text;
using System.Threading;

namespace OrostrideToolkit.Core
{
    public static class StringOptimize
    {
        private static readonly ThreadLocal<StringBuilder> _threadLocalBuilder =
            new(() => new StringBuilder(256));

        public static string Build(string s1, string s2)
        {
            var sb = GetBuilder();
            try {
                sb.Append(s1).Append(s2);
                return sb.ToString();
            } finally { Recycle(sb); }
        }

        public static string Build(string s1, string s2, string s3)
        {
            var sb = GetBuilder();
            try {
                sb.Append(s1).Append(s2).Append(s3);
                return sb.ToString();
            } finally { Recycle(sb); }
        }

        public static string Build(params string[] args)
        {
            var sb = GetBuilder();
            try {
                foreach (var arg in args) sb.Append(arg);
                return sb.ToString();
            } finally { Recycle(sb); }
        }
        
        private static StringBuilder GetBuilder()
        {
            var sb = _threadLocalBuilder.Value;
            sb.Clear();
            return sb;
        }
        private static void Recycle(StringBuilder sb)
        {
            if (sb.Capacity > 1024) sb.Capacity = 256;
            sb.Clear();
        }
    }
}