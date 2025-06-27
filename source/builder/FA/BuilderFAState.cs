//
#nullable disable

using AFX;
using System.Diagnostics;
using System.Runtime.CompilerServices;


//
//
namespace gpp.builder;


//TODO  Нужно проверить действительно нужен ли сортированный массив
internal class BuilderFAAcceptSymbolset
{
  private List<int> m_data;
  //
  public BuilderFAAcceptSymbolset()    => m_data = new();
  //
  public void Add(table_index_t symbol_index_)
  {
    int insertion_cookie = m_data.BinarySearch(symbol_index_);
    if (insertion_cookie < 0)
      m_data.Insert(~insertion_cookie, symbol_index_);
  }
  public int Count()                  => m_data.Count;
  public int this[int index_]         => m_data[index_];
  public void Clear()                 => m_data.Clear();
}


//TODO  Необходимость сортированного массива??
//      Или хэшсет или вообще битмап просится, т.к. фа-индексов конкретное кол-во и относительно немного 
internal class BuilderFAStatesIndexSet
{
  private List<int> m_data;
  //
  public BuilderFAStatesIndexSet()
  {
    m_data = new();
  }
  //
  public void Add(int fa_index_)
  {
    int insertion_cookie = m_data.BinarySearch(fa_index_);
    if (insertion_cookie < 0)
      m_data.Insert(~insertion_cookie, fa_index_);
  }
  public bool Contains(int fa_index_)
  {
    return (m_data.BinarySearch(fa_index_) >= 0);
  }
  public int Count()          => m_data.Count;
  public int this[int index_] => m_data[index_];
  public void Clear()         => m_data.Clear();
  public bool UnionWith(BuilderFAStatesIndexSet other_)
  {
    bool b_union_performed = false;
    for (int i = 0; i < other_.Count(); ++i)
    {
      int value = other_[i];
      int insertion_cookie = m_data.BinarySearch(value);
      if (insertion_cookie < 0)
      {
        m_data.Insert(~insertion_cookie, value);
        b_union_performed = true;
      }
    }
    return b_union_performed;
  }
  public bool IsEqualSet(BuilderFAStatesIndexSet other_)
  {
    if(ReferenceEquals(this, other_)) 
      return true;
    int cnt = m_data.Count;
    if (cnt != other_.m_data.Count)
      return false;

    for (int i = 0; i < cnt; ++i)
      if (this.m_data[i] != other_.m_data[i])
        return false;

    return true;
  }
}



internal class BuilderFAState
{
  // основные
  private short                    m_TableIndex;
  private BuilderSymbol?           m_AcceptSymbol;
  private BuilderFAEdgesList       m_Edges;
  // построитель
  public BuilderFAStatesIndexSet   NFAStates;
  public BuilderFAStatesIndexSet   NFAClosure;
  public BuilderFAAcceptList       AcceptList;
  // для ..View
  public BuilderFAStatesIndexSet   PriorStates;  
  //  
  protected BuilderFAState(table_index_t table_index_, BuilderSymbol? accept_symbol_or_null_)
  {
    this.m_TableIndex     = to_short(table_index_);
    this.m_AcceptSymbol   = accept_symbol_or_null_;
    this.m_Edges          = new BuilderFAEdgesList();
    //
    this.NFAStates        = new();
    this.NFAClosure       = new();
    this.AcceptList       = new BuilderFAAcceptList();
    //
    this.PriorStates      = new();    
  }
  public BuilderFAState()
    :this(TABLE_INDEX_DEFAULT, null)
  { }
  //
  public BuilderFAEdgesList  Edges()      => m_Edges;
  public BuilderSymbol?   Accept          => m_AcceptSymbol;
  public void SetAcceptSymbol(BuilderSymbol accept_symbol_)
  {
    m_AcceptSymbol = accept_symbol_;
  }
  public short TableIndex                 => m_TableIndex;
  public void SetTableIndex(table_index_t table_index_)
  {
    m_TableIndex = to_short(table_index_);
  }

  public void PerformCaseClosureForEdges(UnicodeTable unicode_table_)
  {
    for (int edge_index = 0; edge_index < this.Edges().Count(); ++edge_index)
      this.Edges()[edge_index].Characters.PerformCaseClosure(unicode_table_);
  }
  public void PerformMappingClosureForEdges(UnicodeTable unicode_table_)
  {
    for (int edge_index = 0; edge_index < this.Edges().Count(); ++edge_index)
      this.Edges()[edge_index].Characters.PerformMappingClosure(unicode_table_);
  }


  private class BuilderFAEdgeEqComparer : AFX.IEqualityComparerAB<BuilderFAEdge, BuilderFAEdge>
  {
    bool IEqualityComparerAB<BuilderFAEdge, BuilderFAEdge>.IsEqual(ref readonly BuilderFAEdge a_item_, ref readonly BuilderFAEdge b_item_)
    {
      return a_item_.TargetFAStateIndex == b_item_.TargetFAStateIndex;
    }
    public static BuilderFAEdgeEqComparer Instance = new();
  }
  private class BuilderFAEdgeEqComparerWithStateIndex : AFX.IEqualityComparerAB<BuilderFAEdge, table_index_t>
  {
    bool IEqualityComparerAB<BuilderFAEdge, table_index_t>.IsEqual(ref readonly BuilderFAEdge a_item_, ref readonly table_index_t b_item_)
    {
      return a_item_.TargetFAStateIndex == b_item_;
    }
    public static BuilderFAEdgeEqComparerWithStateIndex Instance = new();
  }
  //
  private AFX.THashSetSimpleUnsafe<BuilderFAEdge> m_edges_index_by_target_fa_state_hash = new(BuilderFAEdgeEqComparer.Instance);
  private void add_to_list_and_index(BuilderFAEdge item_)
  {
    this.Edges().AddFromMyOwnerState(item_);
    m_edges_index_by_target_fa_state_hash.Add(item_.TargetFAStateIndex.GetHashCode(), ref item_);
  }




  public void CreateNewEdgeLambda(int target_fa_state_)
  {
    //TODO  Здесь если запретить добавлять дубли, то все !неполучается! - !not equal!
    //      Возможно "Lambda" и пустой чарсет указывает на замыкание на себя.. Короче разбираться надо...
    // по старому алгоритму, если чарсет пуст, то он добавляется безусловно
    // наш новый чарсет заведомо пуст поэтому мы его добавим напрямую безусловно

    add_to_list_and_index(new BuilderFAEdge(new BuilderFACharset(), target_fa_state_));
  }
  public void CreateNewEdge(BuilderFACharset charset_, int target_fa_state_)
  {
    //TODO  как бы по старому алгоритму если у нас уже есть target_fa_state_, то объект создавать ненужно, а нужно только объединить чарсеты
    //      НО! входящий чарсет тоже может быть пуст - и тогда его нужно добавлять безусловно !!!
    Debug.Assert(!charset_.IsEmpty());
    Debug.Assert(m_Edges.Count() == m_edges_index_by_target_fa_state_hash.Count());

    if(m_edges_index_by_target_fa_state_hash.Count() == 0)
      add_to_list_and_index(new BuilderFAEdge(charset_, target_fa_state_));

    ref readonly BuilderFAEdge existing_edge = ref m_edges_index_by_target_fa_state_hash.TryGetValueRefUnsafe(target_fa_state_.GetHashCode(), ref target_fa_state_, BuilderFAEdgeEqComparerWithStateIndex.Instance);
    if (Unsafe.IsNullRef<BuilderFAEdge>(in existing_edge))
      add_to_list_and_index(new BuilderFAEdge(charset_, target_fa_state_));
    else
      existing_edge.Characters.SetUnionWith(charset_);
  }

}


internal class BuilderFAStatesList
{
  protected readonly  List<BuilderFAState>  m_data;
  private             table_index_t         m_InitialState;
  //
  protected BuilderFAStatesList(int size_)
  {
    m_data          = new List<BuilderFAState>(size_);
    //TODO  По логике бюлдера изначально InitialState должен быть установлен в 0 для _DFA.
    //      Он его принудительно не устанавливает
    //      (для _NFA похоже вообще не используется)
    //      Лоадер устанавливает отдельно через SetInitialState
    m_InitialState = 0;
  }
  //
  public BuilderFAStatesList()
    :this(0)
  { }
  public size_t Count                       => m_data.Count;
  public  BuilderFAState this[int index_]   => m_data[index_];
  public table_index_t InitialState         => m_InitialState;
  public int Add(BuilderFAState item_)
  {
    int index = m_data.Count;
    m_data.Add(item_);
    return index;
  }
  //
  protected void SetInitialState(table_index_t initial_state_)
  { 
    m_InitialState = initial_state_;
  }
}
