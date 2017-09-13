namespace IDisposableAnalyzers
{
    internal class EnumerableType : QualifiedType
    {
        internal static readonly EnumerableType Default = new EnumerableType();

        internal readonly QualifiedMethod Aggregate;
        internal readonly QualifiedMethod All;
        internal readonly QualifiedMethod Any;
        internal readonly QualifiedMethod AsEnumerable;
        internal readonly QualifiedMethod Average;
        internal readonly QualifiedMethod Cast;
        internal readonly QualifiedMethod Concat;
        internal readonly QualifiedMethod Contains;
        internal readonly QualifiedMethod Count;
        internal readonly QualifiedMethod DefaultIfEmpty;
        internal readonly QualifiedMethod Distinct;
        internal readonly QualifiedMethod ElementAt;
        internal readonly QualifiedMethod ElementAtOrDefault;
        internal readonly QualifiedMethod Empty;
        internal readonly QualifiedMethod Except;
        internal readonly QualifiedMethod First;
        internal readonly QualifiedMethod FirstOrDefault;
        internal readonly QualifiedMethod GroupBy;
        internal readonly QualifiedMethod GroupJoin;
        internal readonly QualifiedMethod Intersect;
        internal readonly QualifiedMethod Join;
        internal readonly QualifiedMethod Last;
        internal readonly QualifiedMethod LastOrDefault;
        internal readonly QualifiedMethod LongCount;
        internal readonly QualifiedMethod Max;
        internal readonly QualifiedMethod Min;
        internal readonly QualifiedMethod OfType;
        internal readonly QualifiedMethod OrderBy;
        internal readonly QualifiedMethod OrderByDescending;
        internal readonly QualifiedMethod Repeat;
        internal readonly QualifiedMethod Reverse;
        internal readonly QualifiedMethod Select;
        internal readonly QualifiedMethod SelectMany;
        internal readonly QualifiedMethod SequenceEqual;
        internal readonly QualifiedMethod Single;
        internal readonly QualifiedMethod SingleOrDefault;
        internal readonly QualifiedMethod Skip;
        internal readonly QualifiedMethod SkipWhile;
        internal readonly QualifiedMethod Sum;
        internal readonly QualifiedMethod Take;
        internal readonly QualifiedMethod TakeWhile;
        internal readonly QualifiedMethod ThenBy;
        internal readonly QualifiedMethod ThenByDescending;
        internal readonly QualifiedMethod ToArray;
        internal readonly QualifiedMethod ToDictionary;
        internal readonly QualifiedMethod ToList;
        internal readonly QualifiedMethod ToLookup;
        internal readonly QualifiedMethod Union;
        internal readonly QualifiedMethod Where;
        internal readonly QualifiedMethod Zip;

        public EnumerableType()
            : base("System.Linq.Enumerable")
        {
            this.Aggregate = new QualifiedMethod(this, nameof(this.Aggregate));
            this.All = new QualifiedMethod(this, nameof(this.All));
            this.Any = new QualifiedMethod(this, nameof(this.Any));
            this.AsEnumerable = new QualifiedMethod(this, nameof(this.AsEnumerable));
            this.Average = new QualifiedMethod(this, nameof(this.Average));
            this.Cast = new QualifiedMethod(this, nameof(this.Cast));
            this.Concat = new QualifiedMethod(this, nameof(this.Concat));
            this.Contains = new QualifiedMethod(this, nameof(this.Contains));
            this.Count = new QualifiedMethod(this, nameof(this.Count));
            this.DefaultIfEmpty = new QualifiedMethod(this, nameof(this.DefaultIfEmpty));
            this.Distinct = new QualifiedMethod(this, nameof(this.Distinct));
            this.ElementAt = new QualifiedMethod(this, nameof(this.ElementAt));
            this.ElementAtOrDefault = new QualifiedMethod(this, nameof(this.ElementAtOrDefault));
            this.Empty = new QualifiedMethod(this, nameof(this.Empty));
            this.Except = new QualifiedMethod(this, nameof(this.Except));
            this.First = new QualifiedMethod(this, nameof(this.First));
            this.FirstOrDefault = new QualifiedMethod(this, nameof(this.FirstOrDefault));
            this.GroupBy = new QualifiedMethod(this, nameof(this.GroupBy));
            this.GroupJoin = new QualifiedMethod(this, nameof(this.GroupJoin));
            this.Intersect = new QualifiedMethod(this, nameof(this.Intersect));
            this.Join = new QualifiedMethod(this, nameof(this.Join));
            this.Last = new QualifiedMethod(this, nameof(this.Last));
            this.LastOrDefault = new QualifiedMethod(this, nameof(this.LastOrDefault));
            this.LongCount = new QualifiedMethod(this, nameof(this.LongCount));
            this.Max = new QualifiedMethod(this, nameof(this.Max));
            this.Min = new QualifiedMethod(this, nameof(this.Min));
            this.OfType = new QualifiedMethod(this, nameof(this.OfType));
            this.OrderBy = new QualifiedMethod(this, nameof(this.OrderBy));
            this.OrderByDescending = new QualifiedMethod(this, nameof(this.OrderByDescending));
            this.Repeat = new QualifiedMethod(this, nameof(this.Repeat));
            this.Reverse = new QualifiedMethod(this, nameof(this.Reverse));
            this.Select = new QualifiedMethod(this, nameof(this.Select));
            this.SelectMany = new QualifiedMethod(this, nameof(this.SelectMany));
            this.SequenceEqual = new QualifiedMethod(this, nameof(this.SequenceEqual));
            this.Single = new QualifiedMethod(this, nameof(this.Single));
            this.SingleOrDefault = new QualifiedMethod(this, nameof(this.SingleOrDefault));
            this.Skip = new QualifiedMethod(this, nameof(this.Skip));
            this.SkipWhile = new QualifiedMethod(this, nameof(this.SkipWhile));
            this.Sum = new QualifiedMethod(this, nameof(this.Sum));
            this.Take = new QualifiedMethod(this, nameof(this.Take));
            this.TakeWhile = new QualifiedMethod(this, nameof(this.TakeWhile));
            this.ThenBy = new QualifiedMethod(this, nameof(this.ThenBy));
            this.ThenByDescending = new QualifiedMethod(this, nameof(this.ThenByDescending));
            this.ToArray = new QualifiedMethod(this, nameof(this.ToArray));
            this.ToDictionary = new QualifiedMethod(this, nameof(this.ToDictionary));
            this.ToList = new QualifiedMethod(this, nameof(this.ToList));
            this.ToLookup = new QualifiedMethod(this, nameof(this.ToLookup));
            this.Union = new QualifiedMethod(this, nameof(this.Union));
            this.Where = new QualifiedMethod(this, nameof(this.Where));
            this.Zip = new QualifiedMethod(this, nameof(this.Zip));
        }
    }
}