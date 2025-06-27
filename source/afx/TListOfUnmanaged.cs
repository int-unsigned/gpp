//
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

//
//
namespace AFX;


internal class TSortedListOfSimpleIntegral<TItemSimpleIntegral>
  where TItemSimpleIntegral : unmanaged
{
  protected List<TItemSimpleIntegral> m_data;
  //
  public TSortedListOfSimpleIntegral()
  {
    m_data = new();
  }
  public TItemSimpleIntegral this[int index_]
  {
    get => m_data[index_];
  }
  public void Add(TItemSimpleIntegral value_)
  {
    int insert_index = m_data.BinarySearch(value_);
    if (insert_index < 0)
      m_data.Insert(~insert_index, value_);
  }
  public int Count                                => m_data.Count;
  public bool Contains(TItemSimpleIntegral item_) => (m_data.BinarySearch(item_) >= 0);
  //
}




internal class TListOfStructSimple<TItem> where TItem : unmanaged
{
  private const int _default_capacity = 4;
  internal TItem[]  _items;
  internal int      _size;
  //
  private static readonly TItem[] s_emptyArray = new TItem[0];
  //
  public TListOfStructSimple()
  {
    _items = s_emptyArray;
  }
  public TListOfStructSimple(TListOfStructSimple<TItem> other_)
  {
    int count = other_.Count;
    if (count == 0)
      _items = s_emptyArray;
    else
    {
      _items = new TItem[count];
      _copy(other_._items, 0, _items, 0, count);      
      _size = count;
    }
  }
  public TListOfStructSimple(int capacity_)
  {
    Debug.Assert(capacity_ >= 0);

    if (capacity_ == 0)
      _items = s_emptyArray;
    else
      _items = new TItem[capacity_];
  }
  //
  public int Count    => _size;
  public int Capacity { get => _items.Length; }
  private void GrowCapacityExact(size_t capacity_)
  {
    Debug.Assert(capacity_ > 0);
    Debug.Assert(capacity_ >= _size);

    if (capacity_ > _items.Length)
    {
      TItem[] new_items = new TItem[capacity_];
      if (_size > 0)
        _copy(_items, 0, new_items, 0, _size);

      _items = new_items;
    }
  }
  public int EnsureCapacity(int capacity_)
  {
    Debug.Assert(capacity_ >= 0);

    if (_items.Length < capacity_)
      Grow(capacity_);

    return _items.Length;
  }
  internal void Grow(int requare_capacity_)
  {
    Debug.Assert(_items.Length < requare_capacity_);
    GrowCapacityExact(calculate_capacity_for_grow(requare_capacity_));
  }
  public ref TItem this[int index_]
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get
    {
      Debug.Assert((uint)index_ >= 0 && (uint)index_ < _size);
      //
      //https://tooslowexception.com/getting-rid-of-array-bound-checks-ref-returns-and-net-5/
      ref var array_data = ref MemoryMarshal.GetArrayDataReference(_items);
      return ref Unsafe.Add(ref array_data, (nint)(uint)index_);
    }
  }
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Add(TItem item_)
  {
    if ((uint)_size < (uint)_items.Length)
      _items[_size++] = item_;
    else
      AddWithResize(item_);
  }
  // Non-inline from List.Add to improve its code quality as uncommon path
  [MethodImpl(MethodImplOptions.NoInlining)]
  private void AddWithResize(TItem item)
  {
    Debug.Assert(_size == _items.Length);
    Grow(_size + 1);
    _items[_size++] = item;
  }
  public void Insert(int index_, TItem item_)
  {
    // Note that insertions at the end are !NOT! legal !!! Use Add insteed !! 
    Debug.Assert(index_ >= 0 && index_ < _size);

    if (_size < _items.Length)
      _copy(index_, index_ + 1, _size - index_);
    else
    {
      size_t new_capacity = calculate_capacity_for_grow(_size + 1);
      Debug.Assert(new_capacity > _items.Length);
      TItem[] new_items = new TItem[new_capacity];
      if (index_ == 0)
        _copy(_items, 0, new_items, 1, _size);
      else
      {
        _copy(_items, 0,      new_items, 0,         index_);
        _copy(_items, index_, new_items, index_ + 1, _size - index_);
      }
      _items = new_items;
    }
    //
    ++_size;
    //
    _items[index_] = item_;
  }
  public void RemoveAt(int index_)
  {
    Debug.Assert(index_ < _size);

    _size--;
    //
    if (index_ < _size)
      _copy(index_ + 1, index_, _size - index_);
  }
  public void RemoveRange(int index_, int count_)
  {
    Debug.Assert(index_ >= 0);
    Debug.Assert(count_ > 0);
    Debug.Assert(index_ + count_ <= _size);

    _size -= count_;
    //
    if (index_ < _size)
      _copy(index_ + count_, index_, _size - index_);
  }
  public unsafe bool IsEqualBytes(TListOfStructSimple<TItem> other_)
  {
    Debug.Assert(other_ != null);
    Debug.Assert(!ReferenceEquals(this, other_));

    //https://habr.com/ru/articles/214841/

    if (other_._size != _size)
      return false;
    if (_size == 0)
      return true;

    ref TItem a_data  = ref MemoryMarshal.GetArrayDataReference(_items);
    ref byte a_bytes  = ref Unsafe.As<TItem, byte>(ref a_data);
    ref TItem b_data  = ref MemoryMarshal.GetArrayDataReference(other_._items);
    ref byte b_bytes  = ref Unsafe.As<TItem, byte>(ref b_data);
    uint cb           = (uint)_size * (uint)Unsafe.SizeOf<TItem>();

    fixed (byte* p1 = &a_bytes, p2 = &b_bytes)
    {
      byte* x1 = p1, x2 = p2;

      for (int i = 0; i < cb / 8; i++, x1 += 8, x2 += 8)
        if (*((long*)x1) != *((long*)x2))
          return false;
      if ((cb & 4) != 0)
      {
        if (*((int*)x1) != *((int*)x2))
          return false;
        x1 += 4;
        x2 += 4;
      }
      if ((cb & 2) != 0)
      {
        if (*((short*)x1) != *((short*)x2))
          return false;
        x1 += 2;
        x2 += 2;
      }
      if ((cb & 1) != 0)
        if (*((byte*)x1) != *((byte*)x2))
          return false;

      return true;
    }
  }
  //
  // private:
  private size_t calculate_capacity_for_grow(int requare_capacity_)
  {
    int new_capacity = (_items.Length == 0) ? _default_capacity : 2 * _items.Length;

    if ((uint)new_capacity > Array.MaxLength)
      new_capacity = Array.MaxLength;

    if (new_capacity < requare_capacity_)
      new_capacity = requare_capacity_;

    return new_capacity;
  }
  private void _copy(size_t src_index_, size_t dst_index_, size_t copy_items_count_)
  {
    ref TItem array_data = ref MemoryMarshal.GetArrayDataReference(_items);
    _copy_mem(ref array_data, (uint)src_index_, ref array_data, (uint)_items.Length, (uint)dst_index_, (uint)copy_items_count_);
  }
  private static unsafe void _copy(TItem[] src_, size_t src_index_, TItem[] dst_, size_t dst_index_, size_t copy_items_count_)
  {
    ref TItem array_data_src = ref MemoryMarshal.GetArrayDataReference(src_);
    ref TItem array_data_dst = ref MemoryMarshal.GetArrayDataReference(dst_);
    _copy_mem(ref array_data_src, (uint)src_index_, ref array_data_dst, (uint)dst_.Length, (uint)dst_index_, (uint)copy_items_count_);
  }
  private static unsafe void _copy_mem(ref TItem array_data_src, nuint src_index_, ref TItem array_data_dst, nuint dst_length_, nuint dst_index_, nuint copy_items_count_)
  {
    nuint     cb_element_size   = (nuint)Unsafe.SizeOf<TItem>();
    ref TItem to_copy_src_item  = ref Unsafe.AddByteOffset(ref array_data_src, (uint)src_index_ * cb_element_size);
    ref TItem to_copy_dst_item  = ref Unsafe.AddByteOffset(ref array_data_dst, (uint)dst_index_ * cb_element_size);
    fixed (void* to_copy_src_item_ptr = &to_copy_src_item, to_copy_dst_item_ptr = &to_copy_dst_item)
    {
      Debug.Assert(dst_index_ < dst_length_);
      nuint cb_dst_arailable_size = (dst_length_ - dst_index_) * cb_element_size;
      Buffer.MemoryCopy(to_copy_src_item_ptr, to_copy_dst_item_ptr, cb_dst_arailable_size, copy_items_count_ * cb_element_size);
    }
    //Buffer.MemoryCopy(Unsafe.AsPointer(ref to_copy_src_item), Unsafe.AsPointer(ref to_copy_dst_item), cb_bytes_to_copy, cb_bytes_to_copy);
  }
}

