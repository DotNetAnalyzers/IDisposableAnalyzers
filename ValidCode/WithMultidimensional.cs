namespace ValidCode
{
    public class WithMultidimensional
    {
        public WithMultidimensional()
        {
            this.Data2D = new int[3, 3];
            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < 3; ++j)
                {
                    this.Data2D[i, j] = i * j;
                }
            }
        }

        public int[,] Data2D { get; set; }
    }
}