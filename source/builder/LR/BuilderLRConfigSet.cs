//
using System.Diagnostics;


//
//
namespace gpp.builder;


internal class BuilderLRConfigSet 
{
  private AFX.TArrayUniqueByKeyWithUnion<LRConfig>  m_data;
  //
  private const int                                 m_hash_start  = (int)0xDEADBEE;
  private int                                       m_hash        = m_hash_start;
  public BuilderLRConfigSet()
  {
    m_data = new AFX.TArrayUniqueByKeyWithUnion<LRConfig>();
  }
  //
  public size_t Count()                   => m_data.Count();
  public LRConfig this[size_t index_]     => m_data[index_];
  public void Add(LRConfig item_)
  {
    if (m_data.Add(item_))
      m_hash = HashCode.Combine(m_hash, item_.GetHash());
  }
  public void Clear()                     => m_data.Clear();
  //
  public bool UnionWith(BuilderLRConfigSet other_)
  {
    int my_old_count = m_data.Count();
    bool b_union_performed = m_data.UnionWith(other_.m_data);
    if (b_union_performed && m_data.Count() != my_old_count) 
    {
      Debug.Assert(m_data.Count() > my_old_count);
      int new_hash = m_hash_start;
      for (int i = 0; i < m_data.Count(); ++i)
        new_hash = HashCode.Combine(new_hash, m_data[i].GetHash());
      m_hash = new_hash;
    }
    return b_union_performed;
  }
  public int GetHash() => m_hash;
  //
  public bool IsEqualBaseTo(BuilderLRConfigSet other_config_set_)
  {
    Debug.Assert(other_config_set_ != null);

    int count = m_data.Count();
    if (count != other_config_set_.m_data.Count())
      return false;

    for(int i = 0; i < count; ++i)
      if (!m_data[i].IsEqualKeyTo(other_config_set_.m_data[i]))
        return false;

    return true;
  }
  public static bool IsEqualBase(BuilderLRConfigSet a_configset_, BuilderLRConfigSet b_configset_)
  {
    Debug.Assert(a_configset_ != null);
    return a_configset_.IsEqualBaseTo(b_configset_);
  }


  // Данный метод нужен построителю.
  // Он при сравнении учитывает кроме ключевых полей LRConfig.ParentProduction_.TableIndex и LRConfig.ParserPosition 
  // еще и LRConfig.LookaheadSet
  public LRConfigCompare CompareCore(BuilderLRConfigSet other_)
  {
    //TODO  в наш хэш включаются ключевые поля LRConfig.ParentProduction_.TableIndex и LRConfig.ParserPosition
    //      поэтому (теоретически) если хэш не равен, то весь набор .UnEqual
    //      цикл крутить будем тольк при коллизиях
    //TODO  Желательно протестировать все это получше. Возможно примитивное строение хэш нужно поменять.
    if (m_hash != other_.m_hash)
      return LRConfigCompare.UnEqual;

    // мы массив с ключем уникальности. поэтому если количества не равны - мы заведомо .UnEqual
    int count = m_data.Count();
    if (count != other_.m_data.Count())
      return LRConfigCompare.UnEqual;

    bool b_has_NotEqualLookahead    = false;
    bool b_has_ProperSubset         = false;
    //
    for (int i = 0; i < count; ++i)
    {
      // .CompareCore возвращает LRConfigCompare.EqualBaseNotEqualLookahead если .TableIndex и .ParserPosition равны, но .LookaheadSet никак не равен (.UnEqual)
      // прокрутить цикл по всем мы должны полностью до конца, т.к. всегда может встретиться .UnEqual 
      switch (m_data[i].CompareCore(other_.m_data[i]))
      {
        case LRConfigCompare.ProperSubset:
          b_has_ProperSubset      = true;
          break;
        case LRConfigCompare.EqualBaseNotEqualLookahead:
          b_has_NotEqualLookahead = true;
          break;
        case LRConfigCompare.UnEqual:
          return LRConfigCompare.UnEqual;          
      }
    }

    // здесь, если встречалось .UnEqual, то его уже вернули
    if (b_has_NotEqualLookahead)
      return LRConfigCompare.EqualBaseNotEqualLookahead;
    else if (b_has_ProperSubset)
      return LRConfigCompare.ProperSubset;
    else
      return LRConfigCompare.EqualFull;
  }
}
