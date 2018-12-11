namespace WoSS.Containers
{
    public class QuadTreeFactory<T> where T : IQuadTreeItem
    {
        #region Singleton
        private static QuadTreeFactory<T> instance = null;
        private static readonly object padlock = new object();

        private QuadTreeFactory()
        {
        }

        /// <summary>
        /// Retrieves or create this factory's instance
        /// </summary>
        public static QuadTreeFactory<T> Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new QuadTreeFactory<T>();

                        // Init instance
                        instance.Reset();
                    }
                    return instance;
                }
            }
        }
        #endregion

        #region Properties
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public QuadTreeCapacity Capacity { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Reset factory to default values
        /// </summary>
        public void Reset()
        {
            X = 0f;
            Y = 0f;
            Width = 1f;
            Height = 1f;
            Capacity = QuadTreeCapacity.Medium;
        }

        /// <summary>
        /// Returns a freshly build QuadTreeNode root based on the factory properties
        /// </summary>
        /// <returns></returns>
        public QuadTreeNode<T> Build()
        {
            return QuadTreeNode<T>.BuildNewRoot(Capacity, Width, Height, X, Y);
        }
        #endregion
    }
}
