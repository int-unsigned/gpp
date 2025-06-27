//
using System.Diagnostics;

//
//
namespace AFX;



public enum SetCompareEnum
{
  Equal,
  UnEqual,
  Subset,
}


internal interface TArrayUniqueByKeyWithUnionItem<TItem> : IComparable<TItem>
{
  public bool UnionWithOther(TItem item_);
}


internal static class UnionHelper
{
  public static bool UnionEqualItems<TItem>(TItem a_item_, TItem b_item_) where TItem : TArrayUniqueByKeyWithUnionItem<TItem>
  { 
    return a_item_.UnionWithOther(b_item_);
  }
}


internal class TArrayUniqueByKeyWithUnion<TItem> where TItem : TArrayUniqueByKeyWithUnionItem<TItem>
{
  private List<TItem> m_data;
  //
  public TArrayUniqueByKeyWithUnion()
  {
    m_data = new List<TItem>();
  }
  public TArrayUniqueByKeyWithUnion(TArrayUniqueByKeyWithUnion<TItem> other_)
  {
    foreach (var it in other_.m_data)
      Debug.Assert(it != null);

    m_data = new List<TItem>(other_.m_data);
  }
  public int Count()  => m_data.Count;
  public void Clear() => m_data.Clear(); 
  public bool Add(TItem item_)
  { 
    Debug.Assert(item_ != null);

    int my_item_index = m_data.BinarySearch(item_);
    if (my_item_index >= 0)
      return m_data[my_item_index].UnionWithOther(item_);
    m_data.Insert(~my_item_index, item_);
    return true;
  }
  public bool Contains(TItem item_)
  {
    return (m_data.BinarySearch(item_) >= 0);
  }
  public List<TItem>.Enumerator GetEnumerator() { return m_data.GetEnumerator(); }
  public TItem this[int array_index_]
  {
    get { return m_data[array_index_]; }
  }
  //TODO  По задумке DictionarySet true должно возвращаться в случае любого вида объединения - и если добавлен новвый и если объединились данные имеющихся
  //      Но по алгоритму в LookaheadSymbol::DictionarySet.MemberResult DictionarySet.IMember.Union всегда создается new ConfigTrackSet(this.Configs, lookaheadSymbol.Configs)
  //      и факт их объединения не заносится в поле DictionarySet.MemberResult.SetChanged
  //      Поэтому такое "скрытое" UnionWith вызывающему алгоритму невидно
  //      НО! Факт возврата true там кое-где учитывают!
  //
  //TODO  Все же нужно отказаться от List<TItem> и переделать на собственный типа NumberSet - неудобно - нет прямого длоступа к _count и _data
  public bool UnionWith(TArrayUniqueByKeyWithUnion<TItem> other_)
  {
    uint        union_array_size = unchecked((uint)this.Count()) + unchecked((uint)other_.Count());
    List<TItem> union_array = new List<TItem>((int)union_array_size);
    for (int i = 0; i < union_array_size; ++i)
      union_array.Add(default(TItem));

    uint union_array_count = 0;
    bool b_union_performed = _union_no_check_resize(union_array, ref union_array_count, this.m_data, (uint)this.Count(), other_.m_data, (uint)other_.Count());
    if (union_array_count < union_array_size)
    { 
      uint tail_count_to_remove = union_array_size - union_array_count;
      for (uint i = 0; i < tail_count_to_remove; ++i)
        union_array.RemoveAt(union_array.Count() - 1);
    }        

    if (b_union_performed) 
      m_data = union_array;
    
    return b_union_performed;
  }
  private static bool _union_no_check_resize(List<TItem> union_result_, ref uint in_out_union_result_count_, List<TItem> a_, uint a_count_, List<TItem> b_, uint b_count_)
  {
    bool b_union_performed = false;
    uint a_index = 0;
    uint b_index = 0;
    while (a_index < a_count_ || b_index < b_count_)
    {
      if (a_index >= a_count_)        // если мы закончились, то следующий элемент всегда из набора Другого - значит мы просто должны добавить отстаток от Другого и выйти
      {
        _copy_tail_no_check_resize(union_result_, ref in_out_union_result_count_, b_, b_index, b_count_);
        b_union_performed = true;
        break;
      }
      else if (b_index >= b_count_)  // если Другой закончился, то следующий элемено всегда из нашего набора - значит мы должны добавить остаток из Нашего набора и выйти
      {
        _copy_tail_no_check_resize(union_result_, ref in_out_union_result_count_, a_, a_index, a_count_);
        break;
      }
      else
      { //TODO  Оптимальней на каждой точке сравнения зацикливаться (типа "пока равно", "пока больше ..),
        //      определять количество элементов подлежащих вставке и разом вставлять весь блок
        //      Тогда .ResizeArray будет вызываться один раз на весь вставляемый блок, а не каждый раз +1
        //      (хотя так как наша емкость растет блоками по m_BlockSize это не столь существенно)
        TItem a_item = a_[(int)a_index];
        TItem b_item = b_[(int)b_index];
        int cmp = a_item.CompareTo(b_item);
        if (cmp == 0)
        { // иначе, ни мы ни Другой не закончились и наши элементы равны -> тупо берем в качестве очередного наш элемент. по сути без разницы - можно и Другой - они равны
          _push_back_no_check_resize(union_result_, ref in_out_union_result_count_, a_item);
          if (UnionHelper.UnionEqualItems(a_item, b_item))
            b_union_performed = true;
          ++a_index;
          ++b_index;
        }
        else if (cmp < 0)
        { // на очередном шаге наши элементы не равны и наш элемент меньше Другого - Мы сортированный массив и должны в качестве очередного элемента брать меньший - Наш
          _push_back_no_check_resize(union_result_, ref in_out_union_result_count_, a_item);
          ++a_index;
        }
        else /*cmp > 0*/
        { // и наконец, здесь наш элемент больше Другого (или элемент Другого меньше Нашего) - берем меньший - эемент Другого
          _push_back_no_check_resize(union_result_, ref in_out_union_result_count_, b_item);
          ++b_index;
          b_union_performed = true;
        }
      }
    }
    return b_union_performed;
  }
  private static void _copy_tail_no_check_resize(List<TItem> target_, ref uint target_count_, List<TItem> source_, uint source_start_index_, uint count_)
  {
    for (uint i = source_start_index_; i < count_; ++i)
      _push_back_no_check_resize(target_, ref target_count_, source_[(int)i]);
  }
  private static void _push_back_no_check_resize(List<TItem> data_, ref uint in_out_data_count_, TItem value_)
  {
    Debug.Assert(value_ != null );

    data_[(int)in_out_data_count_] = value_;
    ++in_out_data_count_;
  }
} //class TArrayUniqueByKeyWithUnion<TItem> where TItem : TArrayItemInterface<TItem>
