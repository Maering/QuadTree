using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WoSS.Containers
{

    public enum QuadTreeCapacity
    {
        /// <summary>
        /// 16 objects at top level
        /// </summary>
        Tiny = 16,

        /// <summary>
        /// 64 objects at top level
        /// </summary>
        Small = 64,

        /// <summary>
        /// 256 objects at top level
        /// </summary>
        Medium = 256,

        /// <summary>
        /// 1024 objects at top level
        /// </summary>
        Large = 1024,

        /// <summary>
        /// 4096 objects at top level
        /// </summary>
        Huge = 4096,

        /// <summary>
        /// 16384 objects at top level
        /// </summary>
        Mega = 16384
    }

    public class QuadTreeNode<T> where T : IQuadTreeItem
    {
        private readonly float _x;
        private readonly float _y;
        private readonly float _width;
        private readonly float _height;
        private readonly float _width_half;
        private readonly float _height_half;
        private readonly float _left;
        private readonly float _right;
        private readonly float _up;
        private readonly float _down;
        private readonly int _capacity;
        private bool _isLeaf;

        private readonly QuadTreeNode<T>[] _children;
        private readonly Dictionary<QuadTreeKey, T> _items;

        /// <summary>
        /// Coordinates of the center on the X Axis
        /// </summary>
        public float X { get { return _x; } }

        /// <summary>
        /// Coordinates of the center on the Y Axis
        /// </summary>
        public float Y { get { return _y; } }

        public float Width { get { return _width; } }
        public float Height { get { return _height; } }
        public float WidthHalf { get { return _width_half; } }
        public float HeightHalf { get { return _height_half; } }

        /// <summary>
        /// Left-most correct coordinate, Inclusive
        /// </summary>
        public float Left { get { return _left; } }

        /// <summary>
        /// Right-most correct coordinate, Exclusive
        /// </summary>
        public float Right { get { return _right; } }

        /// <summary>
        /// Top-most correct coordinate, Inclusive
        /// </summary>
        public float Up { get { return _up; } }

        /// <summary>
        /// Bottom-most correct coordinate, Exclusive
        /// </summary>
        public float Down { get { return _down; } }

        /// <summary>
        /// Returns current depth of the node, starts at 0 with the root
        /// </summary>
        public uint Depth { get; private set; }

        /// <summary>
        /// Amount of items that this node can hold
        /// </summary>
        public int Capacity { get { return _capacity; } }

        /// <summary>
        /// Items count for this node
        /// </summary>
        public int Volume { get { return _items.Count; } }

        public bool IsLeaf { get { return _isLeaf; } }

        private QuadTreeNode(int capacity, float width, float height, float x, float y)
        {
            // Capactiy half of previous or minimum 8
            this._capacity = capacity;

            // Width and Height
            this._width = width;
            this._height = height;

            // Half width and height
            this._width_half = this._width / 2.0f;
            this._height_half = this._height / 2.0f;

            // Position
            this._x = x;
            this._y = y;

            // Corners
            this._left = this._x - this._width_half;
            this._right = this._x + this._width_half;
            this._up = this._y - this._height_half;
            this._down = this._y + this._height_half;

            // Childrens
            this._children = new QuadTreeNode<T>[4];

            // Items
            _items = new Dictionary<QuadTreeKey, T>(this.Capacity);

            // Final state
            _isLeaf = true;
        }

        /// <summary>
        /// Create a new node
        /// <para/> From   0 to 512 use Medium
        /// <para/> From 513 to +∞  use Mega
        /// </summary>
        /// <param name="Parent">Parent of this node</param>
        /// <param name="index">
        /// Index: 0/1/2/3
        /// <para/>0: upper left,
        /// <para/>1: upper right,
        /// <para/>2: bottom left,
        /// <para/>3: bottom right,
        /// </param>
        private QuadTreeNode(QuadTreeNode<T> Parent, int index)
            : this(
                      Parent._capacity, // > (int)QuadTreeCapacity.Tiny ? Parent._capacity >> 1 : Parent._capacity,
                      Parent._width / 2.0f,
                      Parent._height / 2.0f,
                      index % 2 == 0 ? Parent._x - (Parent._width_half / 2f) : Parent._x + (Parent._width_half / 2f),
                      index < 2      ? Parent._y - (Parent._height_half / 2f) : Parent._y + (Parent._height_half / 2f)
                  )
        {
            // Depth
            this.Depth = Parent.Depth + 1;
        }

        /// <summary>
        /// Returns a new instance of QuadTreeNode<typeparamref name="T"/>
        /// <para/>Used to make a new QuadTree
        /// </summary>
        /// <param name="capacity">Max capacity of a single leaf, it is strongly recommanded you leave it by default. Performs better</param>
        /// <param name="width">Width of the square where items will lies</param>
        /// <param name="height">Height of the square where items will lies</param>
        /// <param name="x">Coordinate of X-Axis of the square where items will lies</param>
        /// <param name="y">Coordinate of Y-Axis of the square where items will lies</param>
        /// <returns></returns>
        public static QuadTreeNode<T> BuildNewRoot(QuadTreeCapacity capacity = QuadTreeCapacity.Medium, float width = float.MaxValue, float height = float.MaxValue, float x = 0f, float y = 0f)
        {
            return new QuadTreeNode<T>((int)capacity, width, height, x, y);
        }

        /// <summary>
        /// Will try to insert an item inside the quadTree
        /// <para/>Inside itself if it has enough space or it will generate 4 child and insert in one of these
        /// <para/>Recursive operation
        /// </summary>
        /// <param name="item">Item to insert</param>
        /// <returns>True if item was inserted</returns>
        public bool Insert(T item)
        {
            // Get final node where item will be Inserted
            var node = this.GetNode(item.X, item.Y);

            // If node is null, we won't be able to insert
            if (node == null)
                return false;

            // Check if the leaf is full
            if (node.IsFull())
            {
                // Create children
                for (int i = 0; i < 4; i++)
                {
                    node._children[i] = new QuadTreeNode<T>(node, i);
                }

                // Switch to a node
                node._isLeaf = false;

                // Move every child
                int index = 0;
                foreach (var kv in node._items)
                {
                    index = node.GetIndexFromCoordinates(kv.Key.X, kv.Key.Y);
                    node._children[index].Insert(kv.Value); // Maybe protected this if it returns false. It should not but anyway ...
                }

                // Remove every item in this node
                node._items.Clear();

                // Finally insert new item
                return node.Insert(item);
            }
            else
            {
                return node.Add(item);
            }
        }

        /// <summary>
        /// Try to insert an item
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <param name="item">Item to insert</param>
        /// <returns>True if item has been inserted</returns>
        private bool Add(T item)
        {
            // Generate a key for the new location
            var key = new QuadTreeKey(item.X, item.Y);

            // Try to insert item
            if (_items.ContainsKey(key))
            {
                return false;
            }
            else
            {
                _items.Add(key, item);
                return true;
            }
        }

        /// <summary>
        /// Look for the given item. Compare with .Equals(obj)
        /// </summary>
        /// <param name="searchedItem">What to search</param>
        /// <param name="index">Where it lies</param>
        /// <returns>True if any matching item was found</returns>
        public bool Contains(T searchedItem, out QuadTreeKey index)
        {
            unchecked {

                index = null;
                bool b = false;

                if (this._isLeaf)
                {
                    var iter = _items.GetEnumerator();
                    KeyValuePair<QuadTreeKey, T> kv = default(KeyValuePair<QuadTreeKey, T>);
                    while (!b && iter.MoveNext())
                    {
                        kv = iter.Current;
                        b |= kv.Value.Equals(searchedItem);
                    }

                    // Assign output key
                    index = kv.Key;
                }
                else
                { 
                    // Will search in every children
                    var childrenIter = _children.GetEnumerator();
                    QuadTreeNode<T> node = default(QuadTreeNode<T>);
                    while (!b && childrenIter.MoveNext())
                    {
                        node = childrenIter.Current as QuadTreeNode<T>;
                        b |= node.Contains(searchedItem, out index);
                    }                   
                }

                return b;
            }
        }

        /// <summary>
        /// Search for any item at given coordinates
        /// </summary>
        /// <param name="x">Coordinate on X Axis</param>
        /// <param name="y">Coordinate on Y Axis</param>
        /// <param name="item">Item found at given coordinates or default value of item's type</param>
        /// <returns>True if any matching item was found</returns>
        public bool Find(float x, float y, out T item)
        {
            if(IsInside(x, y))
            {
                if (this._isLeaf)
                {
                    return _items.TryGetValue(new QuadTreeKey(x, y), out item);
                }
                else
                {
                    // Move to matching node
                    var node = GetNode(x, y);
                    return node.Find(x, y, out item);
                }
            }
            else
            {
                item = default(T);
                return false;
            }
        }

        /// <summary>
        /// Returns a key generated from the given item
        /// </summary>
        /// <param name="item">Item used to generate the key</param>
        /// <returns>QuadTreeKey generated from the given item</returns>
        public QuadTreeKey GetKeyFromItem(T item)
        {
            return new QuadTreeKey(item.X, item.Y);
        }

        /// <summary>
        /// Look for items at given coordinates
        /// </summary>
        /// <param name="x">Coordinate on X Axis, center of the circle</param>
        /// <param name="y">Coordinate on Y Axis, center of the circle</param>
        /// <param name="radius">Radius of the circle</param>
        /// <param name="matchingItems">Items found at given coordinates</param>
        /// <returns>Collection of items inside the given circle</returns>
        public bool Find(float x, float y, float radius, out ICollection<T> matchingItems)
        {
            bool b = IsInside(x, y);
            matchingItems = new List<T>(Volume);

            unchecked {
                
                if (b)
                {
                    if (this._isLeaf)
                    {
                        foreach (QuadTreeKey key in _items.Keys)
                        {
                            if (Math.Sqrt(((x - key.X) * (x - key.X)) + ((y - key.Y) * (y - key.Y))) <= radius)
                                matchingItems.Add(_items[key]);
                        }
                    }
                    else
                    {
                        ICollection<T> matchingChildrenItems;
                        // Goes through every child of this node
                        foreach (var child in _children)
                        {
                            child.Find(x, y, radius, out matchingChildrenItems);
                            foreach (T item in matchingChildrenItems)
                                matchingItems.Add(item);
                        }
                    }
                }
            }

            return b;
        }

        /// <summary>
        /// Look for items matching the predicate
        /// </summary>
        /// <param name="predicate">Condition to fulfill</param>
        /// <returns>Collection of items matching the predicate</returns>
        public ICollection<T> Where(Predicate<T> predicate)
        {
            unchecked
            {
                List<T> matching_items = new List<T>(Volume);

                if (this._isLeaf)
                {
                    // Goes through every item stored in this node
                    foreach(var kv in _items)
                    {
                        if(predicate.Invoke(kv.Value))
                            matching_items.Add(kv.Value);
                    }
                }
                else
                {
                    // Goes through every child of this node
                    foreach (var child in _children)
                    {
                        matching_items.AddRange(child.Where(predicate));
                    }
                }

                return matching_items;
            }
        }

        /// <summary>
        /// Look for the first item matching the predicate
        /// </summary>
        /// <param name="predicate">Condition to fulfill</param>
        /// <returns>Collection of items matching the predicate</returns>
        public T First(Predicate<T> predicate)
        {
            T item = default(T);
            unchecked
            {
                if (this._isLeaf)
                {
                    // Goes through every item stored in this node
                    foreach (var kv in _items)
                    {
                        if (predicate.Invoke(kv.Value))
                            return kv.Value;
                    }
                }
                else
                {
                    // Goes through every child of this node
                    foreach (var child in _children)
                    {
                        item = child.First(predicate);
                        if (item != null)
                            break;
                    }
                }
            }
            return item;
        }

        /// <summary>
        /// Remove all items for this node and its children
        /// </summary>
        public void Clear()
        {
            unchecked
            {
                if (this._isLeaf)
                {
                    _items.Clear();

                    // Becomes a node
                    this._isLeaf = false;
                }
                else
                {
                    for (int i = 0; i < _children.Length; i++)
                    {
                        _children[i].Clear();
                        _children[i] = null;
                    }

                    // Becomes a leaf
                    this._isLeaf = true;
                }
            }
        }

        /// <summary>
        /// Search for the more precise node at given coordinates
        /// </summary>
        /// <param name="x">Coordinate on X Axis, center of the circle</param>
        /// <param name="y">Coordinate on Y Axis, center of the circle</param>
        /// <returns>Returns the matching node</returns>
        public QuadTreeNode<T> GetNode(float x, float y)
        {
            if(IsInside(x, y))
            {
                if(this._isLeaf)
                {
                    return this;
                }
                else
                {
                    int index = GetIndexFromCoordinates(x, y);
                    return _children[index].GetNode(x, y);
                }
            }
            return null;
        }

        /// <summary>
        /// Goes through every children and returns every item found
        /// </summary>
        /// <returns>An Enumerable of all stored items</returns>
        public IEnumerable<T> All()
        {
            unchecked
            {
                if (this._isLeaf)
                {
                    return this._items.Values;
                }
                else
                {
                    List<T> matching_items = new List<T>(Volume);

                    // Goes through every child of this node
                    foreach (var child in _children)
                    {
                        matching_items.AddRange(child.All());
                    }

                    return matching_items;
                }
            }
        }

        /// <summary>
        /// Goes through every children and sum total items count
        /// </summary>
        /// <returns>Total items stored</returns>
        public int Count()
        {
            unchecked
            {
                if (this._isLeaf)
                {
                    return this.Volume;
                }
                else
                {
                    int count = 0;

                    // Goes through every child of this node
                    foreach (var child in _children)
                    {
                        count += child.Count();
                    }
                    return count;
                }
            }
        }

        /// <summary>
        /// Check if coordinates are inside the square
        /// </summary>
        /// <param name="x">Coordinate on X Axis</param>
        /// <param name="y">Coordinate on Y Axis</param>
        /// <returns>True if coordinates are inside the square</returns>
        public bool IsInside(float x, float y)
        {
            return (x >= this._left && x < this._right) && (y >= this._up && y < this._down);
        }

        /// <summary>
        /// Check if this node is full
        /// </summary>
        /// <returns>True if volume has reached maximum capacity</returns>
        public bool IsFull()
        {
            return Volume >= Capacity;
        }

        /// <summary>
        /// Check if this node contains no item
        /// </summary>
        /// <returns>True if volume is exactly equal to 0</returns>
        public bool IsEmpty()
        {
            return Volume == 0;
        }

        /// <summary>
        /// Search for the index
        /// <para/>WARNING : No OutOfBorderException will be thrown, please make sure x and y are inside
        /// <para/>         -  You can use IsInside(x, y) as an helper function in order to achieve that
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int GetIndexFromCoordinates(float x, float y)
        {
            if (this._isLeaf)
                return -1;

            int col = x < this._x ? 0 : 1;
            int row = y < this._y ? 0 : 1;

            return col + (row * 2);
        }
    }
}
