//
using System.Diagnostics;
using System.Numerics;

//
//
namespace AFX;


public struct BitMask32
{
  private uint m_data = 0;
  public BitMask32() 
  { }
  //
  public static uint BitmaskForIndex(uint index_)
  {
    Debug.Assert(index_ < 32);
    return 1U << (int)index_;
  }
  public bool Get(uint index_)
  {
    return (m_data & BitmaskForIndex(index_)) != 0;
  }
  public void Set(uint index_)
  {
    m_data |= BitmaskForIndex(index_);
  }
  public void Del(uint index_)
  {
    m_data &= ~BitmaskForIndex(index_);
  }
}


internal struct BitMask64
{
  private ulong m_data = 0;
  public BitMask64()  //TODO  я обязан объявить пустой конструктор т.к. у поля m_data = 0 есть инициализатор. инициализатор - это часть конструктора
  { }                 //      на заметку - если я не буду объявлять ни конструктор ни инициализатор, то массив структур можно инициализировать просто обнуляя (заполняя) память
  //
  public static ulong BitmaskForIndex(uint index_)
  {
    Debug.Assert(index_ < 64);
    return 1UL << (/*byte*/int)index_;
  }
  public readonly bool Equals(BitMask64 other_) => (m_data == other_.m_data);
  public readonly bool Get(uint index_)
  {
    return (m_data & BitmaskForIndex(index_)) != 0;
  }
  public readonly uint GetBitFirst()
  {
    if (m_data == 0)
      return uint.MaxValue;
    else
      return (uint)BitOperations.TrailingZeroCount(m_data);
  }
  public readonly uint GetBitNext(uint bit_prev_)  
  {
    Debug.Assert(bit_prev_ < 63);   // на входе bit_prev_ 0-62, т.к. для 63 следующий бит просто не существует
    Debug.Assert(m_data != 0);      // какой-то bit_prev_ у нас установлен

    ulong reset_bit   = 1UL << unchecked((int)bit_prev_);   // из индекса бита делаем маску бита
    ulong reset_mask  = reset_bit | (reset_bit - 1);        // маска сброса всех битов меньших или равных 
    ulong data        = m_data & ~reset_mask;
    if (data == 0)
      return uint.MaxValue;
    else
      return (uint)BitOperations.TrailingZeroCount(data);
  }
  public void Set(uint index_)
  {
    m_data |= BitmaskForIndex(index_);
  }
  public void SetRange(uint index_lo_, uint index_hi_)
  {
    Debug.Assert(index_lo_ <= 63);
    Debug.Assert(index_hi_ <= 63);
    Debug.Assert(index_lo_ <= index_hi_);
    ulong mask_lo = (1UL << unchecked((int)index_lo_)) - 1;
    ulong bit_hi  = (1UL << unchecked((int)index_hi_));
    ulong mask_hi = (bit_hi - 1) | bit_hi;
    ulong mask    = mask_hi & ~mask_lo;
    m_data |= mask;
  }
  public void Set(ref readonly BitMask64 other_)
  {
    m_data |= other_.m_data;
  }
  public void Del(uint index_)
  {
    m_data &= ~BitmaskForIndex(index_);
  }  
  public void Del(ref readonly BitMask64 other_)
  {
    m_data &= ~other_.m_data;
  }
  public void Assign(ref readonly BitMask64 other_)
  {
    m_data = other_.m_data;
  }
  public readonly size_t CountBitsSet_1()
  {
    return BitOperations.PopCount(m_data);
  }
  public readonly bool IsAllSet_0()
  {
    return (m_data == 0);
  }
}



internal struct BitMask256
{
  private BitMask64 m_data_0;
  private BitMask64 m_data_1;
  private BitMask64 m_data_2;
  private BitMask64 m_data_3;
  //
  public const uint MaxIndex = 255;
  //
  public BitMask256 MakeCopy()
  {
    BitMask256 copy;
    copy.m_data_0 = this.m_data_0;
    copy.m_data_1 = this.m_data_1;
    copy.m_data_2 = this.m_data_2;
    copy.m_data_3 = this.m_data_3;
    return copy;
  }
  private static uint PartIndexFormIndex(uint index_)
  {
    Debug.Assert(index_ < 256);
    return index_ >> 6;
  }
  public readonly bool IsEqual(BitMask256 other_) => 
    (m_data_0.Equals(other_.m_data_0) 
    && m_data_1.Equals(other_.m_data_1) 
    && m_data_2.Equals(other_.m_data_2) 
    && m_data_3.Equals(other_.m_data_3));
  public readonly bool Get(uint index_)
  {
    switch (PartIndexFormIndex(index_))
    {
      case 0: return m_data_0.Get(index_ - 0 * 64);
      case 1: return m_data_1.Get(index_ - 1 * 64);
      case 2: return m_data_2.Get(index_ - 2 * 64);
      default /*case 3*/: return m_data_3.Get(index_ - 3 * 64);
    }
  }
  public void Set(uint index_)
  {
    switch (PartIndexFormIndex(index_))
    {
      case 0: m_data_0.Set(index_ - 0 * 64); break;
      case 1: m_data_1.Set(index_ - 1 * 64); break;
      case 2: m_data_2.Set(index_ - 2 * 64); break;
      default /*case 3*/: m_data_3.Set(index_ - 3 * 64); break;
    }
  }
  public void Add(uint index_) => Set(index_);

  private bool _add_range_ex(ref BitMask64 data_part_, uint part_min_, uint part_max_, uint lo_, uint index_last_)
  {
    if (index_last_ <= part_max_)
    {
      data_part_.SetRange(lo_, index_last_ - part_min_);
      return true;
    }
    else
    {
      data_part_.SetRange(lo_, 63);
      return false;
    }
  }
  private bool _add_range(ref BitMask64 data_part_, uint part_min_, uint part_max_, uint index_1st_, uint index_last_)
  {
    uint lo = (index_1st_ < part_min_) ? 0 : index_1st_ - part_min_;
    return _add_range_ex(ref data_part_, part_min_, part_max_, lo, index_last_);
  }
  public void AddRange(uint index_1st_, uint index_last_)
  {
    switch (PartIndexFormIndex(index_1st_))
    { 
      case 0:   if (_add_range_ex(ref m_data_0, 64 * 0, 64 * 1 - 1, index_1st_, index_last_))
                  break;
                else
                  goto case 1;
      case 1:   if (_add_range(   ref m_data_1, 64 * 1, 64 * 2 - 1, index_1st_, index_last_))
                  break;
                else
                  goto case 2;
      case 2:   if (_add_range(   ref m_data_2, 64 * 2, 64 * 3 - 1, index_1st_, index_last_))
                  break;
                else
                  goto case 3;
      case 3:
                  Debug.Assert(index_last_ <= 255);
                  _add_range(     ref m_data_3, 64 * 3, 64 * 4 - 1, index_1st_, index_last_);
                  break;
    }      
  }
  public void Del(uint index_)
  {
    switch (PartIndexFormIndex(index_))
    {
      case 0: m_data_0.Del(index_ - 0 * 64); break;
      case 1: m_data_1.Del(index_ - 1 * 64); break;
      case 2: m_data_2.Del(index_ - 2 * 64); break;
      default /*case 3*/: m_data_3.Del(index_ - 3 * 64); break;
    }
  }
  public void Set(ref readonly BitMask256 other_)
  {
    m_data_0.Set(in other_.m_data_0);
    m_data_1.Set(in other_.m_data_1);
    m_data_2.Set(in other_.m_data_2);
    m_data_3.Set(in other_.m_data_3);
  }
  
  public void Del(ref readonly BitMask256 other_)
  {
    m_data_0.Del(in other_.m_data_0);
    m_data_1.Del(in other_.m_data_1);
    m_data_2.Del(in other_.m_data_2);
    m_data_3.Del(in other_.m_data_3);
  }


  // sugar
  public void SetUnionWith(ref readonly BitMask256 other_)      => Set(in other_);
  public void SetDifferenceWith(ref readonly BitMask256 other_) => Del(in other_);
  public void Remove(ref readonly BitMask256 other_)            => Del(in other_);


  private readonly uint GetBitFirstFrom(uint section_index_)
  {
    uint b;
    switch (section_index_)
    {
      case 0: b = m_data_0.GetBitFirst(); if (b == uint.MaxValue) goto case 1; else return b + 0 * 64;
      case 1: b = m_data_1.GetBitFirst(); if (b == uint.MaxValue) goto case 2; else return b + 1 * 64;
      case 2: b = m_data_2.GetBitFirst(); if (b == uint.MaxValue) goto case 3; else return b + 2 * 64;
      case 3: b = m_data_3.GetBitFirst(); if (b == uint.MaxValue) return b;    else return b + 3 * 64;
    }
    Debug.Assert(false); //we never be here
    return uint.MaxValue;
  }
  public readonly uint GetBitFirst() => GetBitFirstFrom(0);
  private readonly uint GetBitNextFrom(uint section_index_, uint bit_prev_)
  {
    uint b;
    switch (section_index_)
    {
      case 0: b = m_data_0.GetBitNext(bit_prev_ - 0 * 64); if (b == uint.MaxValue) return GetBitFirstFrom(1); else return b + (0 * 64);
      case 1: b = m_data_1.GetBitNext(bit_prev_ - 1 * 64); if (b == uint.MaxValue) return GetBitFirstFrom(2); else return b + (1 * 64);
      case 2: b = m_data_2.GetBitNext(bit_prev_ - 2 * 64); if (b == uint.MaxValue) return GetBitFirstFrom(3); else return b + (2 * 64);
      case 3: b = m_data_3.GetBitNext(bit_prev_ - 3 * 64); if (b == uint.MaxValue) return uint.MaxValue;      else return b + (3 * 64);
    }
    Debug.Assert(false); //we never be here
    return uint.MaxValue;
  }
  public readonly uint GetBitNext(uint bit_prev_) // от 0 до 254, т.к. если прев 255, то для него некст не существует
  {
    Debug.Assert(bit_prev_ < 255);
    // 1) нужно определить к какой секции относится bit_prev_
    //    у нас 4 секции по 64 бита и нужно делить на 64 или сдвинуть на 6
    uint sect_id = PartIndexFormIndex(bit_prev_);
    switch (sect_id)
    {
      case 0: if (bit_prev_ == 1 * 64 - 1) return GetBitFirstFrom(1); else return GetBitNextFrom(sect_id, bit_prev_);
      case 1: if (bit_prev_ == 2 * 64 - 1) return GetBitFirstFrom(2); else return GetBitNextFrom(sect_id, bit_prev_);
      case 2: if (bit_prev_ == 3 * 64 - 1) return GetBitFirstFrom(3); else return GetBitNextFrom(sect_id, bit_prev_);
      case 3: return GetBitNextFrom(sect_id, bit_prev_);
    }
    Debug.Assert(false); //we never be here
    return uint.MaxValue;
  }
  public readonly size_t CountBitsSet_1()
  {
    return m_data_0.CountBitsSet_1() + m_data_1.CountBitsSet_1() + m_data_2.CountBitsSet_1() + m_data_3.CountBitsSet_1();
  }
  public readonly bool IsAllSet_0()
  {
    return m_data_0.IsAllSet_0() && m_data_1.IsAllSet_0() && m_data_2.IsAllSet_0() && m_data_3.IsAllSet_0();
  }
  public void AssignCopy(ref readonly BitMask256 other_)
  {
    m_data_0.Assign(in other_.m_data_0);
    m_data_1.Assign(in other_.m_data_1);
    m_data_2.Assign(in other_.m_data_2);
    m_data_3.Assign(in other_.m_data_3);
  }
  //TODO  Пока для нас мув нет (только копи), но поддерживаем интерфейс
  //TODO  Если поддерживать интерфейс, то другого надо сбросить - мало ли на что вызывающий рассчитывает..
  public void AssignMove(ref BitMask256 other_) => AssignCopy(in other_);
} //BitMask256

