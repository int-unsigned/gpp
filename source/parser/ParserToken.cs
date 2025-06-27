//
using System.Diagnostics;

//
//
namespace gpp.parser;


public struct ParserPosition
{
  public int Line;
  public int Column;
  //
  public ParserPosition()
  {
    this.Line   = 0;
    this.Column = 0;
  }
  internal void Assign(ParserPosition other_pos_)
  {
    this.Column   = other_pos_.Column;
    this.Line     = other_pos_.Line;
  }
}


//TODO  nullable контекст здесь надо доработать. 
//      фактически проблема в конструкторе без параметров (CreateInitialToken)
//      и прилепленных свойствах от парент-симбола
//      во всех случаях кроме InitialToken симбол должен быть не нулл, а InitialToken должен обрабатываться особо
//      (пс. смотреть как я это в плюсах сделал)
//#nullable disable
//
public class ParserToken
{
  private short           m_State;
  private object?         m_Data;
  private ParserSymbol?   m_Parent;
  private ParserPosition  m_Position;

  //TODO  используется исключительно для .m_stack.Push(new Token(this.m_Tables.LALR.InitialState)
  //      и помнится оч.важно чтобы m_Parent был нулл
  //      то есть здесь нужен не публичный конструктор, а фактори-метод CreateInitialToken
  //TODO  Также из за этой логики шарп не верит, что во всех остальных случаях у нас m_Parent НЕ НУЛЛ
  internal ParserToken(short state_)
  {
    this.m_Position = new ParserPosition();
    this.m_Parent   = null;
    this.m_Data     = null;
    this.m_State    = state_;
  }
  public ParserToken(ParserSymbol Parent, ParserReduction data_)
  {
    this.m_Position = new ParserPosition();
    this.m_Parent   = Parent;
    this.m_Data     = data_;
    this.m_State    = (short) 0;
  }
  public ParserToken(ParserSymbol symbol_, string data_, ParserPosition position_)
  {
    this.m_Position = position_;
    this.m_Parent   = symbol_;
    this.m_Data     = data_;
    this.m_State    = (short)0;
  }

  public ParserPosition Position
  {
    get => this.m_Position;
    set => this.m_Position = value;
  }
  public object? Data
  {
    get => this.m_Data;
    set => this.m_Data = value;
  }
  internal short State
  {
    get => this.m_State;
    set => this.m_State = value;
  }
  public ParserSymbol? Parent
  {
    get => this.m_Parent;
    set => this.m_Parent = value;
  }

  public SymbolType     Type()        => this.m_Parent.Type;
  internal ParserGroup  Group()       => this.m_Parent.Group;
}


//TODO  Это вообще то приватный для парсера класс. Его бы туда перенести, но там пока и так всего много..
//      Нужно пересмотреть его иницииализацию - дла каждой грамматики известна максимальная глубина стека 
//      (у себя в плюсах посмотреть как сделано)
internal class ParserTokenStack
{
  private Stack<ParserToken> m_stack;
  //
  public ParserTokenStack() => m_stack = new Stack<ParserToken>();
  //
  internal int Count                    => m_stack.Count;
  public void Clear()                   => m_stack.Clear();
  public void Push(ParserToken token_)  => m_stack.Push(token_);
  public ParserToken Pop()              => m_stack.Pop();
  public ParserToken Top()              => m_stack.Peek();
}



//  Стек на основе списка, который растет вниз - т.е. голова списка где Top, Pop и Push находятся в конце массива
//  (массив сам аггрессивно подстраивает свою емкость, чтобы аллокаций меньше было)
//  Для исключения сделан метод InsertHead, который вставляет элемент в голову (начало, конец..) - короче в противоположный Top-Pop-Push конец списка
//  это для редко используемой операции EnqueueInput
internal class ParserTokenDeque
{
  private List<ParserToken> m_data;  
  //
  public ParserTokenDeque() => m_data = new List<ParserToken>();
  //
  internal bool Empty()         => (this.m_data.Count == 0); 
  public void Clear()           => this.m_data.Clear();
  public ParserToken? Top()     => ((m_data.Count == 0)? null : m_data[m_data.Count - 1]);
  public ParserToken Pop()
  { 
    Debug.Assert(m_data.Count > 0);
    // как и у "взрослых" мы не проверяем есть у нас Top или нет - это дело вызывающего.
    int   index_last  = m_data.Count - 1;
    ParserToken t           = m_data[index_last];
    // при удалении последнего List ничего не копирует и не переаллоцирует.
    m_data.RemoveAt(index_last);    
    return t;
  }
  public void Push(ParserToken token_)        => m_data.Add(token_);
  public void InsertHead(ParserToken token_)  => m_data.Insert(0, token_);
}
