//
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

//
//
namespace AFX;



//TODO  THashSetSimpleUnsafe


public interface IEqualityComparerAB<TItemA, TItemB>
{
  internal bool IsEqual(ref readonly TItemA a_item_, ref readonly TItemB b_item_);
}


internal class THashSetSimpleUnsafe<TItem>
  where TItem : notnull
{  
  /// <summary>Cutoff point for stackallocs. This corresponds to the number of ints.</summary>
  private const int StackAllocThreshold = 100;

  /// <summary>
  /// When constructing a hashset from an existing collection, it may contain duplicates,
  /// so this is used as the max acceptable excess ratio of capacity to count. Note that
  /// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
  /// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
  /// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
  /// </summary>
  private const int ShrinkThreshold = 3;
  private const int StartOfFreeList = -3;


  private struct Entry
  {
    public int    HashCode;
    /// <summary>
    /// 0-based index of next entry in chain: -1 means end of chain
    /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
    /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
    /// </summary>
    public int    Next;
    public TItem  Value;
  }

  private int[]?    _buckets;
  private Entry[]?  _entries;
#if TARGET_64BIT
  private ulong     _fastModMultiplier;
#endif
  private int       _count;
  private int       _freeList;
  private int       _freeCount;
  //
  private IEqualityComparerAB<TItem, TItem> _comparer;
  public THashSetSimpleUnsafe(int capacity_, [DisallowNull] IEqualityComparerAB<TItem, TItem> comparer_) 
  {
    Debug.Assert(comparer_ != null);
    _comparer = comparer_;

    Debug.Assert(capacity_ >= 0);
    Initialize(capacity_);
  }
  public THashSetSimpleUnsafe([DisallowNull] IEqualityComparerAB<TItem, TItem> comparer_)
    :this(0, comparer_) 
  { }
  //
  //
  public ref TItem Add(int item_hash_, ref readonly TItem item_) 
    => ref AddIfNotPresent(item_hash_, in item_, _comparer);

  public ref TItem TryGetValueRefUnsafe<TItemB>(int hash_, ref readonly TItemB identificator_, [DisallowNull] IEqualityComparerAB<TItem, TItemB> comparer_)
  {
     return ref FindItemRef(hash_, in identificator_, comparer_);
  }

  /// <summary>Removes all elements from the <see cref="HashSet{T}"/> object.</summary>
  public void Clear()
  {
    int count = _count;
    if (count > 0)
    {
      Debug.Assert(_buckets != null, "_buckets should be non-null");
      Debug.Assert(_entries != null, "_entries should be non-null");

      Array.Clear(_buckets);
      _count      = 0;
      _freeList   = -1;
      _freeCount  = 0;
      Array.Clear(_entries, 0, count);
    }
  }
  public int Count()
  { 
    return _count;
  }
  public bool Contains<TItemB>(int hash_, ref readonly TItemB identificator_, [DisallowNull] IEqualityComparerAB<TItem, TItemB> comparer_)    
  {
    ref TItem it = ref FindItemRef(hash_, in identificator_, comparer_);
    if(Unsafe.IsNullRef<TItem>(in it))
      return false;
    else
      return true;
  }


  //
  public Enumerator GetEnumerator() => new Enumerator(this);
  //
  public struct Enumerator //: IEnumerator<TItem>
  {
    private readonly THashSetSimpleUnsafe<TItem>  _hashSet;
    private int                         _index;
    private TItem                       _current;

    internal Enumerator(THashSetSimpleUnsafe<TItem> hashSet)
    {
      _hashSet  = hashSet;
      _index    = 0;
      _current  = default!;
    }

    public bool MoveNext()
    {
      // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
      // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
      while ((uint)_index < (uint)_hashSet._count)
      {
        ref Entry entry = ref _hashSet._entries![_index++];
        if (entry.Next >= -1)
        {
          _current = entry.Value;
          return true;
        }
      }

      _index    = _hashSet._count + 1;
      _current  = default!;
      return false;
    }
    public TItem Current => _current;
    public void Dispose() 
    { }
  }


  //
  [StackTraceHidden]
  internal static class ThrowHelper
  {
    [DoesNotReturn]
    internal static void ThrowInvalidOperationException_ConcurrentOperationsNotSupported()
    {
      throw new InvalidOperationException("ConcurrentOperationsNotSupported");
    }
    [DoesNotReturn]
    internal static void ThrowInvalidOperationException_CapacityOverflow()
    {
      throw new InvalidOperationException("CapacityOverflow");
    }
  }

  //
  private static class HashHelpers
  {
    public const uint HashCollisionThreshold = 100;

    // This is the maximum prime smaller than Array.MaxLength.
    public const int MaxPrimeArrayLength = 0x7FFFFFC3;

    public const int HashPrime = 101;

    // Table of prime numbers to use as hash table sizes.
    // A typical resize algorithm would pick the smallest prime number in this array
    // that is larger than twice the previous capacity.
    // Suppose our Hashtable currently has capacity x and enough elements are added
    // such that a resize needs to occur. Resizing first computes 2x then finds the
    // first prime in the table greater than 2x, i.e. if primes are ordered
    // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
    // Doubling is important for preserving the asymptotic complexity of the
    // hashtable operations such as add.  Having a prime guarantees that double
    // hashing does not lead to infinite loops.  IE, your hash function will be
    // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
    // We prefer the low computation costs of higher prime numbers over the increased
    // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
    internal static ReadOnlySpan<int> Primes =>
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];
    public static bool IsPrime(int candidate)
    {
      if ((candidate & 1) != 0)
      {
        int limit = (int)Math.Sqrt(candidate);
        for (int divisor = 3; divisor <= limit; divisor += 2)
        {
          if ((candidate % divisor) == 0)
            return false;
        }
        return true;
      }
      return candidate == 2;
    }
    public static int GetPrime(int min)
    {
      if (min < 0)
        ThrowHelper.ThrowInvalidOperationException_CapacityOverflow();

      foreach (int prime in Primes)
      {
        if (prime >= min)
          return prime;
      }

      // Outside of our predefined table. Compute the hard way.
      for (int i = (min | 1); i < int.MaxValue; i += 2)
      {
        if (IsPrime(i) && ((i - 1) % HashPrime != 0))
          return i;
      }
      return min;
    }

    // Returns size of hashtable to grow to.
    public static int ExpandPrime(int oldSize)
    {
      int newSize = 2 * oldSize;

      // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
      // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
      if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
      {
        Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
        return MaxPrimeArrayLength;
      }

      return GetPrime(newSize);
    }
  }


  /// <summary>
  /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
  /// greater than or equal to capacity.
  /// </summary>
  private int Initialize(int capacity_)
  {
    int size    = HashHelpers.GetPrime(capacity_);

    // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
    _freeList = -1;
    _buckets  = new int[size];
    _entries  = new Entry[size]; ;
#if TARGET_64BIT
    _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif
    return size;
  }

  private ref TItem AddIfNotPresent(int item_hash_, [DisallowNull] ref readonly TItem value_, [DisallowNull] IEqualityComparerAB<TItem, TItem> comparer_)
  {
    Debug.Assert(value_   != null);
    Debug.Assert(_buckets != null);

    Entry[]? entries = _entries;
    Debug.Assert(entries != null, "expected new_entries to be non-null");

    uint collisionCount = 0;
    ref int ref_bucket  = ref Unsafe.NullRef<int>();

    {
      Debug.Assert(_comparer is not null);
      ref_bucket  = ref GetBucketRef(item_hash_);
      int entry_index = ref_bucket - 1; // Value in _buckets is 1-based
      while (entry_index >= 0)
      {
        ref Entry entry = ref entries[entry_index];
        if (entry.HashCode == item_hash_ && comparer_.IsEqual(in entry.Value!, in value_))
          return ref entry.Value;
        entry_index = entry.Next;

        ++collisionCount;
        if (collisionCount > (uint)entries.Length)    // The chain of entries forms a loop, which means a concurrent update has happened.          
          ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
      }
    }

    int new_item_entry_index;
    if (_freeCount > 0)
    {
      new_item_entry_index = _freeList;
      _freeCount--;
      Debug.Assert((StartOfFreeList - entries![_freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
      _freeList = StartOfFreeList - entries[_freeList].Next;
    }
    else
    {
      int count = _count;
      if (count == entries.Length)
      {
        Resize(HashHelpers.ExpandPrime(_count));
        ref_bucket = ref GetBucketRef(item_hash_);
      }
      new_item_entry_index = count;
      _count = count + 1;
      entries = _entries;
    }

    {
      ref Entry entry = ref entries![new_item_entry_index];
      entry.HashCode  = item_hash_;
      entry.Value     = value_;
      entry.Next      = ref_bucket - 1;             // Value in _buckets is 1-based
      ref_bucket      = new_item_entry_index + 1;   // Value in _buckets is 1-based
      
      // если мы добавили элемент, то возвращаем NullRef !!!
      return ref Unsafe.NullRef<TItem>();
    }    
  }

  private void Resize(int new_size_)
  {
    // We never rehash
    Debug.Assert(_entries != null, "_entries should be non-null");
    Debug.Assert(new_size_ >= _entries.Length);

    var new_entries = new Entry[new_size_];

    int count = _count;
    Array.Copy(_entries, new_entries, count);

    // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
    _buckets = new int[new_size_];
#if TARGET_64BIT
      _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)new_size_);
#endif
    for (int entry_index = 0; entry_index < count; entry_index++)
    {
      ref Entry entry = ref new_entries[entry_index];
      if (entry.Next >= -1)
      {
        ref int ref_entry_bucket  = ref GetBucketRef(entry.HashCode);
        entry.Next                = ref_entry_bucket - 1; // Value in _buckets is 1-based
        ref_entry_bucket          = entry_index + 1;      // Value in _buckets is 1-based
      }
    }

    _entries = new_entries;
  }

  private ref TItem FindItemRef<TItemB>(int hash_, ref readonly TItemB id_, [DisallowNull] IEqualityComparerAB<TItem, TItemB> comparer_)
  {
    Debug.Assert(comparer_ is not null);
    Debug.Assert(_buckets != null && _entries != null, "Expected initialized");

    if(_count == 0)
      return ref Unsafe.NullRef<TItem>();
    
    uint collisionCount = 0;
         
    int entry_index = GetBucketRef(hash_) - 1; // Value in _buckets is 1-based
    while (entry_index >= 0)
    {
      ref Entry entry = ref _entries[entry_index];
      if (entry.HashCode == hash_ && comparer_.IsEqual(in entry.Value, in id_))
        return ref entry.Value;
      else
        entry_index = entry.Next;

      collisionCount++;
      if (collisionCount > (uint)_entries.Length)
      {
        // The chain of entries forms a loop, which means a concurrent update has happened.
        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
      }
    }      

    return ref Unsafe.NullRef<TItem>();
  }

  /// <summary>Gets a reference to the specified hashcode's bucket, containing an index into <see cref="_entries"/>.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private ref int GetBucketRef(int hash_code_)
  {
    int[] buckets = _buckets!;
#if TARGET_64BIT
    return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
#else
    return ref buckets[(uint)hash_code_ % (uint)buckets.Length];
#endif
  }

}

