namespace INTERCAL.Extensions
{
    public static class StringExtensions
    {
        public static string Multiply(this char a, int b)
        {
            var result = "";
            while (b-- > 0) result += a;
            return result;
        }
    }
}