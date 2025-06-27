//

//
//
namespace gpp.builder;


internal class BuilderFAEdge(BuilderFACharset charset_, int target_fa_state_index_)
{
  private BuilderFACharset  m_Charset = charset_;
  private int               m_Target  = target_fa_state_index_;
  //
  public int TargetFAStateIndex       => m_Target;
  public BuilderFACharset Characters  => m_Charset;
  public void SetCharacters(BuilderFACharset charset_)
  {
    m_Charset = charset_;
  }
}


internal class BuilderFAEdgesList
{
  private readonly List<BuilderFAEdge> m_data;
  //
  public BuilderFAEdgesList()               => m_data = new List<BuilderFAEdge>();
  //
  public BuilderFAEdge this[int index_]     => m_data[index_];
  public size_t Count()                     => m_data.Count;
  //TODO  BuilderFAEdgesList глубоко вложен в BuilderFAState и используется по-разному в разных режимах
  //      1) режим View - просто просмотр .egt файла
  //      2) режим построения NFA (когда BuilderFAState это состояние NFA-автомата) - .AddFromMyOwnerState
  //      3) режим построения DFA из NFA (вроде) - .AddFromBuildDFAState
  //      Нужно сначала сделать разные соответствующие режимам BuilderFAState,
  //      а уже потом все эти разномастные Add... сами встанут на свои места
  public void AddFromBuildDFAState(BuilderFAEdge item_)       => m_data.Add(item_);
  //
  public void AddFromMyOwnerState(BuilderFAEdge item_)        => m_data.Add(item_);
  //
  public void AddFromLoader(BuilderFAEdge item_)              => m_data.Add(item_);
}

