//
using System.Diagnostics;

//
//
namespace gpp.parser;


internal class ParserFAEdge(ParserCharset charset_, int target_)
{
  private readonly  ParserCharset   Characters = charset_;
  public readonly   int             Target     = target_;
  //
  public bool Contains(int char_code_) => this.Characters.Contains(char_code_);
}


internal class ParserFAEdgesList
{
  private List<ParserFAEdge> m_data;
  //
  //TODO  В .egt не пишется кол-во edge. там .StoreEmpty() в этом месте видимо "на вырост"
  //      в следующих версиях нужно доработать
  public ParserFAEdgesList()            => this.m_data = new List<ParserFAEdge>();
  public ParserFAEdge this[int index_]  => this.m_data[index_];
  public void Add(ParserFAEdge Edge)    => this.m_data.Add(Edge);
  public int Count()              => this.m_data.Count;
}


internal class ParserFAState(ParserSymbol? accept_symbol_)
{
  public readonly ParserFAEdgesList   EdgeList      = new ParserFAEdgesList();
  public readonly ParserSymbol?             AcceptSymbol  = accept_symbol_;
}


internal class ParserFAStatesList
{
  protected List<ParserFAState>   m_data;
  protected table_index_t         m_InitialState;
  //
  protected ParserFAStatesList(size_t size_)    => m_data = new List<ParserFAState>(size_); 
  public table_index_t InitialState             => m_InitialState;
  public ParserFAState this[int index_]         => m_data[index_];
  public bool IsEmpty()                         => (m_data.Count == 0);
}

