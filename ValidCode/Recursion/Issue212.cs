namespace ValidCode.Recursion
{
    class Issue212
    {
        public void M<T>(out T str)
        {
            str = default;
            //return M(0, out str);
        }

        public T M<T>(int _, out T str)
        {
            str = default;
            return default;
        }
    }
}
