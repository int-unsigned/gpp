//

//
//
namespace gpp.builder;


//TODO  Вроде я здесь битмап хотел?
internal class BuilderGroupNesting : AFX.TSortedListOfSimpleIntegral<table_index_t>
{
  public string Text(string separator_) => base.m_data.ToStringDelimited(separator_);
  public override string ToString()     => this.Text(", ");
}


internal class BuilderGroup 
{
  private table_index_t         m_TableIndex;
  internal string               Name;
  internal BuilderSymbol        Container;
  internal BuilderSymbol        Start;
  internal BuilderSymbol        End;
  internal GroupAdvanceMode     Advance;
  internal GroupEndingMode      Ending;  
  internal string               NestingNames;
  internal bool                 IsBlock;
  private BuilderGroupNesting   m_Nesting;
  //
  public readonly TIdentificator  Identificator;
  //
  public const string DefaultNestingNames = "None";
  //
  protected BuilderGroup(table_index_t table_index_, string name_, BuilderSymbol container_, BuilderSymbol start_symb_, BuilderSymbol end_symb_, GroupAdvanceMode advance_mode_, GroupEndingMode ending_mode_)    
  {
    this.m_TableIndex  = table_index_;
    this.Name          = name_;
    this.Container     = container_;
    this.Start         = start_symb_;
    this.End           = end_symb_;
    this.Advance       = advance_mode_;
    this.Ending        = ending_mode_;
    this.IsBlock       = false;
    this.NestingNames  = BuilderGroup.DefaultNestingNames;   
    this.Identificator = MakeIdentificator(name_);
    //
    m_Nesting           = new();
  }
  //
  public table_index_t        TableIndex  => m_TableIndex; 
  public BuilderGroupNesting  Nesting     => m_Nesting;
  protected void SetTableIndex(int table_index_)
  {
    m_TableIndex = to_short(table_index_);
  }
}


internal class BuilderGroupsList 
{
  protected readonly List<BuilderGroup> m_data;
  //  
  protected BuilderGroupsList(int size_)   => m_data = new List<BuilderGroup>(size_);
  //
  public BuilderGroup this[int index_]                  => m_data[index_];
  public size_t Count                                   => m_data.Count;
  public List<BuilderGroup>.Enumerator GetEnumerator()  => m_data.GetEnumerator();

  //
  //TODO Нужно разобраться с необходимостью именно ItemIndex
  public int ItemIndex(string name_)
  {
    TIdentificator id = MakeIdentificator(name_);
    return ItemIndex(ref id);
  }
  public bool Contains(ref readonly TIdentificator identificator_)
  {
    return (this.ItemIndex(in identificator_) != -1);
  }
  public int ItemIndex(ref readonly TIdentificator identificator_)
  {
    for (int i = 0; i < m_data.Count; i++)
    {
      BuilderGroup item = this[i];
      if (item.Identificator.IsEqual(in identificator_))
        return i;
    }
    return -1;
  }

  public override string ToString() =>  m_data.ToStringDelimited(", ");
}
