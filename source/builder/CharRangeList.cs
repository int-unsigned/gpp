//
using System;
using System.Diagnostics;


//
//
namespace gpp.builder;
//
using range_t           = CharsRange;
using range_list_data_t = AFX.TListOfStructSimple<CharsRange>;


struct CharsRange
{
  private char_t m_begin;
  private char_t m_final;
  //  
  private static bool _valid(char_t char_begin_, char_t char_final_)
  {
    return is_valid_char(char_begin_) && is_valid_char(char_final_) && is_valid_range(char_begin_, char_final_);    
  }
  //
  public readonly char_t char_begin => this.m_begin;
  public void assign_begin(char_t new_begin_)
  {
    Debug.Assert(_valid(new_begin_, m_final));
    this.m_begin = new_begin_;
  }
  public readonly char_t char_final  => this.m_final;
  public void assign_final(char_t new_final_)
  {
    Debug.Assert(_valid(m_begin, new_final_));
    this.m_final= new_final_;
  }
  public readonly bool is_just_before(char_t ch_code_)
  {
    return (char_next(char_final) == ch_code_);
  }
  public readonly bool is_just_after(char_t ch_code_)
  {
    return (char_prev(char_begin) == ch_code_);
  }
  public readonly bool can_merge_begin_with(char_t ch_code_)
  {
    return char_next(ch_code_) == char_begin;
  }
  public readonly bool can_merge_final_with(char_t ch_code_)
  {
    return char_prev(ch_code_) == char_final;
  }
  //
  public static CharsRange construct(char_t ch_begin_, char_t ch_final_) 
  {
    Debug.Assert(_valid(ch_begin_,ch_final_));
    CharsRange range;
    range.m_begin = ch_begin_;
    range.m_final = ch_final_;
    return range;
  }
}


internal class CharsRangeList
{
  private range_list_data_t m_data;
  //
  range_t _new_range(char_t char_begin_, char_t char_final_)
  {
    return range_t.construct(char_begin_, char_final_);
  }
  range_t _new_range(range_t range_)  => _new_range(range_.char_begin, range_.char_final);
  range_t _new_range(char_t char_)    => _new_range(char_, char_);
  //
  //
  public CharsRangeList() 
  {
    m_data = new();
  } 
  //TODO   используется где-то в бюлдере и, вроде !TODO! требует глубокой копии ??
  public CharsRangeList(CharsRangeList other_)
  {    
    m_data = new(other_.m_size);
    for (int i = 0; i < other_.m_size; ++i)
      m_data.Add(_new_range(other_.m_data[i]));
  }


  //
  private size_t m_size               => m_data.Count;
  //
  public int  Count                   => m_size;
  public bool Empty()                 => (m_size == 0);
  public ref range_t this[int index_] => ref m_data[index_];
  public bool Contains(char_t ch_code_)
  {
    return (m_size > 0 && _search(ch_code_) >= 0);
  }
  public size_t CalculateCharsCount()
  {
    size_t count = 0;
    for (int i = 0; i < m_size; ++i)
      count += (m_data[i].char_final - m_data[i].char_begin + 1);

    return count;
  }


  public void AddSequentionalRangeUnsafe(int start_value_, int end_value_)
  {
    // we check only with assert`s. 
    _add(_new_range(start_value_, end_value_));
  }



  private int _search(char_t value_, int lo_index_, int hi_index_)
  {
    Debug.Assert((lo_index_ >= 0 && m_data.Count > 0 && hi_index_ < m_data.Count));

    int _compare_range_with_char(range_t range_, char_t ch_code_)
    {
      if (ch_code_ < range_.char_begin)   // если чар меньша начала диапазона, то, диапазон БОЛЬШЕ чем чар
        return 1;
      if (ch_code_ > range_.char_final)   // если чар больше конца диапазона, то диапазон МЕНЬШЕ чем чар
        return -1;
      return 0;                           // иначе чар попал в диапазон - считаем их "равными" для целей .Contains
    }

    int lo = lo_index_; 
    int hi = hi_index_; // если будешь переходить на uint не забудь изменить алгоритм (зациклишься!)

    while (lo <= hi)
    {
      int mid = lo + ((hi - lo) >> 1);
      int cmp = _compare_range_with_char(m_data[mid], value_); 

      if (cmp == 0)
        return mid;

      if (cmp < 0)
        lo = mid + 1;
      else
        hi = mid - 1;
    }

    return ~lo;
  }
  private int _search(char_t value_, int lo_index_) => this._search(value_, lo_index_, m_size - 1);
  private int _search(char_t ch_code_)              => this._search(ch_code_, 0);



  

  public bool Add(char_t char_code_)
  {
    Debug.Assert(_valid_char(char_code_));

    if (m_size == 0)
      return _add(char_code_);

    int insertion_cookie = _search(char_code_);
    if (insertion_cookie >= 0)
      return false;

    int insertion_pos = ~insertion_cookie;

    if (insertion_pos == 0)
    {
      if (m_data[insertion_pos].can_merge_begin_with(char_code_))
        _assign_begin_at(insertion_pos, char_code_);
      else
        _insert_at(insertion_pos, _new_range(char_code_));
    }
    else if (insertion_pos == m_size)
    {
      if (m_data[m_size - 1].can_merge_final_with(char_code_))
        _assign_final_at(m_size - 1, char_code_);
      else 
        _add(char_code_);        
    }
    else
    { 
      size_t  insertion_next    = insertion_pos;      // Здесь вставка между диапазонами. insertion_pos - это СЛЕДУЮЩИЙ (больший) после char_code_ диапазон
      size_t  insertion_prev    = insertion_pos - 1;  // insertion_pos здесь > 0 т.к. вариант 0 мы проверили вначале
      bool    b_merge_with_prev = m_data[insertion_prev].can_merge_final_with(char_code_);
      bool    b_merge_with_next = m_data[insertion_next].can_merge_begin_with(char_code_);
      if (b_merge_with_prev && b_merge_with_next)
      {
        m_data[insertion_prev].assign_final(m_data[insertion_next].char_final);
        _remove_range(insertion_next);
      }
      else if (b_merge_with_prev)
        _assign_final_at(insertion_prev, char_code_);
      else if (b_merge_with_next)
        _assign_begin_at(insertion_next, char_code_);
      else
        _insert_at(insertion_pos, _new_range(char_code_));
    }

    return true;
  }




  bool _is_equal(ref readonly range_t a, ref readonly range_t b) 
  { 
    return (a.char_begin == b.char_begin && a.char_final == b.char_final);
  }

  public bool IsEqualSetItems(CharsRangeList other_)
  {
    size_t cnt = m_size;
    if(other_.m_size != cnt) 
      return false;

    for (int i = 0; i < cnt; ++i)
      if (!_is_equal(ref m_data[i], ref other_.m_data[i]))
        return false;

    return true;
  }
  public bool IsEqualSet(CharsRangeList other_)
  { 
    //TODO  Нужно окончательно остановиться на одном варианте - по-байтно сравниваем или по-элементно
    bool bb = m_data.IsEqualBytes(other_.m_data);
    //bool bd = IsEqualSetItems(other_);
    //Debug.Assert(bb == bd);
    return bb;
  }



  bool _valid_char(char_t char_code_)                             { return (char_code_ >= 0 && char_code_ <= char.MaxValue); }
  bool _valid(char_t char_begin_, char_t char_final_)             { return (char_final_ >= char_begin_);  }
  bool _valid(range_t range_)                                     { return _valid(range_.char_begin, range_.char_final); }
  bool _valid_at(size_t index_)                                   { return _valid(m_data[index_]); }



  void _insert_at(size_t at_, range_t item_)
  {
    Debug.Assert(_valid(item_));
    Debug.Assert(at_ == 0       || m_data[at_ - 1].char_final < item_.char_begin);
    Debug.Assert(at_ == m_size  || m_data[at_].char_begin > item_.char_final);

    m_data.Insert(at_, item_);
  }
  char_t _next(char_t char_code_)
  { 
    Debug.Assert(char_code_ < char.MaxValue);
    return char_code_ + 1;
  }
  char_t _prev(char_t char_code_)
  {
    Debug.Assert(char_code_ > char.MinValue);
    return char_code_ - 1;
  }
  bool _is_single_char(ref readonly range_t range_)
  {
    Debug.Assert(_valid(range_));
    return (range_.char_begin == range_.char_final);
  }
  bool _is_single_char_at(size_t index_)
  {
    return (m_data[index_].char_begin == m_data[index_].char_final);
  }
  void _decrease_final_at(size_t index_)
  {
    _assign_final_at(index_, _prev(m_data[index_].char_final));
  }
  void _increase_begin_at(size_t index_) 
  {
    _assign_begin_at(index_, _next(m_data[index_].char_begin));
  }
  void _assign_final_at(size_t index_, char_t new_char_final_)
  {    
    Debug.Assert(index_ == m_size - 1 || m_data[index_ + 1].char_begin > new_char_final_);
    m_data[index_].assign_final(new_char_final_);
  }
  void _assign_begin_at(size_t index_, char_t new_char_begin_)
  {    
    Debug.Assert(index_ == 0 || m_data[index_ - 1].char_final < new_char_begin_);
    m_data[index_].assign_begin(new_char_begin_);
  }
  void _remove_range(size_t remove_index_)
  {
    Debug.Assert(remove_index_ >= 0);
    Debug.Assert(remove_index_ < m_size);
    m_data.RemoveAt(remove_index_);
  }
  void _remove_ranges(size_t remove_begin_, size_t remove_final_)
  {
    Debug.Assert(remove_begin_ >= 0);
    Debug.Assert(remove_final_ < m_size);
    Debug.Assert(remove_final_ >= remove_begin_);
    m_data.RemoveRange(remove_begin_, remove_final_ - remove_begin_ + 1);
  }
  bool _remove_ranges_auto(size_t remove_begin_, size_t remove_final_)
  {
    Debug.Assert(remove_begin_ >= 0);
    Debug.Assert(remove_final_ < m_size);
    if (remove_final_ >= remove_begin_)
    {
      _remove_ranges(remove_begin_, remove_final_);
      return true;
    }
    return false;
  }


  public bool Remove(char_t char_code_)
  {
    int pos_ = _search(char_code_);
    if (pos_ < 0) // нет у нас такого чар
      return false;

    _substruct_from_range(pos_, char_code_, char_code_);
    return true;
  }




  private void _split_range_at(size_t index_, char_t range_to_remove_begin_, char_t range_to_remove_final_)
  {
    var new_range_part_2 = _new_range(_next(range_to_remove_final_), m_data[index_].char_final);
    _assign_final_at(index_, _prev(range_to_remove_begin_));
    _insert_at_or_add(index_ + 1, new_range_part_2);
  }
  void _insert_at_or_add(size_t index_, range_t range_) 
  {
    if (index_ == m_size)
      _add(range_);
    else
      _insert_at(index_, range_);
  }

  private void _substruct_from_range(int pos_, char_t range_to_remove_begin_, char_t range_to_remove_final_)
  {
    Debug.Assert(range_to_remove_begin_ >= m_data[pos_].char_begin && range_to_remove_final_ <= m_data[pos_].char_final);

    bool b_eq_begin = (range_to_remove_begin_ == m_data[pos_].char_begin);
    bool b_eq_final = (range_to_remove_final_ == m_data[pos_].char_final);
    if (b_eq_begin && b_eq_final)
      _remove_range(pos_);
    else if (b_eq_begin)
      _assign_begin_at(pos_, _next(range_to_remove_final_));
    else if (b_eq_final)
      _assign_final_at(pos_, _prev(range_to_remove_begin_));
    else
      _split_range_at(pos_, range_to_remove_begin_, range_to_remove_final_);
  }

  private int _substruct_from_range_begin(int pos_begin_, char_t range_to_remove_begin_)
  {
    Debug.Assert(range_to_remove_begin_ >= m_data[pos_begin_].char_begin && range_to_remove_begin_ <= m_data[pos_begin_].char_final);

    int pos_remove_begin;
    if (m_data[pos_begin_].char_begin == range_to_remove_begin_)
      pos_remove_begin = pos_begin_;
    else
    {
      _assign_final_at(pos_begin_, _prev(range_to_remove_begin_));
      pos_remove_begin = pos_begin_ + 1;
    }
    return pos_remove_begin;
  }
  private int _substruct_from_range_final(int pos_final_, char_t range_to_remove_final_)
  {
    Debug.Assert(range_to_remove_final_ >= m_data[pos_final_].char_begin && range_to_remove_final_ <= m_data[pos_final_].char_final);
                                                                      // мы уверены, что точка конца удаления находится внутри этого диапазона
    int pos_remove_final;
    if (m_data[pos_final_].char_final == range_to_remove_final_)      // если точка конца удаления совпадает с концом диапазона
      pos_remove_final = pos_final_;                                  // то этот диапазон подлежит удалению полностью
    else
    {                                                                 // иначе мы устанавливаем началом диапазона следующий за range_to_remove_final_ чар
      _assign_begin_at(pos_final_, _next(range_to_remove_final_));    // таким образом от диапазона остается только конец
      pos_remove_final = pos_final_ - 1;                              // и указываем, что удалять надо до предыдущего этому диапазона
    }
    return pos_remove_final;
  }

  private void _substruct_from_ranges(int pos_begin_, int pos_final_, ref readonly range_t range_to_remove_)
  {
    Debug.Assert(pos_final_ > pos_begin_);

    int pos_remove_begin = _substruct_from_range_begin(pos_begin_, range_to_remove_.char_begin);
    int pos_remove_final = _substruct_from_range_final(pos_final_, range_to_remove_.char_final); ;

    _remove_ranges_auto(pos_remove_begin, pos_remove_final);
  }


  public bool Remove(range_t range_to_remove_)
  {
    if (m_size == 0)
      return false;

    if(_is_single_char(ref range_to_remove_))
      return Remove(range_to_remove_.char_begin);

    int pos_begin = _search(range_to_remove_.char_begin);
    //
    if (pos_begin >= 0)
    {
      int pos_final = _search(range_to_remove_.char_final, pos_begin);
      
      if (pos_final >= 0)
      {
        if (pos_begin == pos_final)
          _substruct_from_range(pos_begin, range_to_remove_.char_begin, range_to_remove_.char_final);   //TODO эта ветка тестами не охвачена
        else  
          _substruct_from_ranges(pos_begin, pos_final, ref range_to_remove_); 
      }
      else                                  // pos_begin >= 0 && pos_final < 0
      {       
        int pos_remove_begin      = _substruct_from_range_begin(pos_begin, range_to_remove_.char_begin);       //TODO эта ветка тестами не охвачена  
        int pos_remove_final_next = ~pos_final;
        _remove_ranges_auto(pos_remove_begin, pos_remove_final_next - 1);      
      }
      return true;
    }
    else                                    // pos_begin < 0
    {                                       // начальная точка удаления попала между диапазонами. (возможно выше первого диапазона или ниже последнего)
      int pos_remove_begin = ~pos_begin;    // значит диапазон полностью БОЛЬШЕ начала удаления и подлежит удалению полностью      
      if (pos_remove_begin >= m_size)       // ..кроме когда точка удаления начала попала ниже последнего диапазона. ничего не удаляется
        return false;

      int pos_final = _search(range_to_remove_.char_final, pos_remove_begin);      
      if (pos_final == pos_begin)           // начало и конец удаляемого диапазона попали в один промежуток между нашими диапазонами. ничего не удаляется
        return false;
              
      if (pos_final >= 0)                   // точка конца удаления попала в валидный диапазон
      {                                     // или корректируем его с конца или он подлежит удалению
        int pos_remove_final = _substruct_from_range_final(pos_final, range_to_remove_.char_final);     //TODO эта ветка тестами не охвачена 
        _remove_ranges_auto(pos_remove_begin, pos_remove_final);
        return true;
      }
      else                                        // pos_begin < 0 && pos_final < 0 и это не один и тот же диапазон
      {                                           // pos_final попал в промежуток между диапазонами. то есть диапазон pos_remove_final_next полностью БОЛЬШЕ range_to_remove_.char_final
        int pos_remove_final_next = ~pos_final;   // соответственно pos_remove_final_next НЕ подлежит корректировке/удалению
                                                  // а предшествующий ему "pos_remove_final_next - 1" подлежит удалению полностью
        Debug.Assert(pos_remove_final_next == m_size || m_data[pos_remove_final_next].char_begin > range_to_remove_.char_final);
        return _remove_ranges_auto(pos_remove_begin, pos_remove_final_next - 1);
      }
    }
  }


  public bool Remove(CharsRangeList range_list_)
  {
    if (range_list_.Count > 0)
    {
      bool b_removed = this.Remove(range_list_[0]);
      for (int i = 1; i < range_list_.Count; ++i)
        b_removed |= this.Remove(range_list_[i]);

      return b_removed;
    }
    else
      return false;
  }
  public void DifferenceWith(CharsRangeList range_list_) => Remove(range_list_);


  public void AssignCopy(CharsRangeList other_)
  {
    //TODO  ну рассчитываю, что шарп умный и сделает здесь именно купию всех NumberRange, а не ссылки на оригиналы добавит..
    m_data = new range_list_data_t(other_.m_data);
  }
  public void AssignMove(CharsRangeList other_)
  {
    m_data = other_.m_data;
    //TODO  А здесь бы хотелось присвоить НУЛЛ, но тогда это нужно учитывать во всех наших методах - что мы совершенно неожиданно оказались НУЛЛ
    //      или может хрен с ним - после мув ЕГО использовать как бы никто не должен..
    other_.m_data = new range_list_data_t();
  }

  bool _add(range_t range_)
  {
    Debug.Assert(_valid(range_));
    Debug.Assert(m_size == 0 || m_data[m_size - 1].char_final < range_.char_begin);
    m_data.Add(range_);
    return true;
  }
  bool _add(char_t char_)
  {
    Debug.Assert(m_size == 0 || m_data[m_size - 1].char_final < char_);
    m_data.Add(_new_range(char_));
    return true;
  }


  public bool AddRange(char_t range_begin_, char_t range_final_)
  {
    if (range_begin_ == range_final_)
      return this.Add(range_begin_);

    if (m_data.Count == 0)
      return _add(_new_range(range_begin_, range_final_));

    Debug.Assert(_valid(range_begin_, range_final_));

    int pos_begin = _search(range_begin_);
    //
    if (pos_begin >= 0)
    { 
      int pos_final = _search(range_final_, pos_begin);       // range_begin_ находится в одном из наших диапазонов
      if (pos_final >= 0)                                     // range_final_ находится в одном из наших диапазонов
      { 
        if (pos_final == pos_begin)                           // если это тот же самый диапазон, что и pos_begin, 
          return false;                                       // ... то ничего не делаем - мы поглощаем добавляемый диапазон

        Debug.Assert(pos_final > pos_begin);

        m_data[pos_begin].assign_final(m_data[pos_final].char_final);   // добавляемый диапазон объединяет все наши диапазоны от pos_begin до pos_final включительно
        _remove_ranges(pos_begin + 1, pos_final);                       // диапазону pos_begin присвоим конец pos_final и удалим все диапазоны от pos_begin + 1, до pos_final включительно
        return true;
      }
      else                                                              // pos_begin >= 0 && pos_final < 0.   range_final_ попал где-то между нашими диапазонами
      {                                                                 // это значит, что в ~pos_final находится диапазон, у которого .char_begin > range_final_                      
        size_t ins_final = ~pos_final;                                  // (.. при этом ~pos_final.char_begin может оказаться смежным с range_final_)
        //
        if (ins_final == m_size)                                              // ins_final за концом всех наших диапазонов (удаление "до конца")
        {
          m_data[pos_begin].assign_final(range_final_);                       // растягиваем pos_begin до конца вставляемого
          _remove_ranges_auto(pos_begin + 1, ins_final - 1);                  // удаляем все лишние после pos_begin
        }
        else if (m_data[ins_final].can_merge_begin_with(range_final_))        // Здесь ins_final валидный диапазон и смежный с range_final_
        {         
          m_data[pos_begin].assign_final(m_data[ins_final].char_final);       // диапазону pos_begin присваеваем конец смежного ins_final (поглощаем его)
          _remove_ranges(pos_begin + 1, ins_final);                           // удаляем все между и включая ins_final - мы его поглотили          
        }
        else                                                                  // Здесь ins_final валидный диапазон, который НЕ смежный с range_final_.
        {
          m_data[pos_begin].assign_final(range_final_);                       // растягиваем pos_begin до range_final_ 
          _remove_ranges_auto(pos_begin + 1, ins_final - 1);                  // и удаляем все от pos_begin + 1 до ins_final - 1
        }
        return true;
      }
    }
    else                                                                      // pos_begin < 0. начальная точка попала в промежуток между нашими диапазонами (или указывает на конец)
    {       
      int ins_begin = ~pos_begin;                                             // то есть в позиции ~pos_begin сейчас находится диапазон у которого .char_begin БОЛЬШЕ чем range_begin_
      //
      if (ins_begin == m_size)                                                // если начальная точка попала в сразу за нашим последним диапазоном, 
        _insert_auto_end(range_begin_, range_final_);                         // ... то просто вставляем в конец (с учетом возможной смежности)
      else if (m_data[ins_begin].char_begin > range_final_)                   // здесь ins_begin - это валидный диапазон, который БОЛЬШЕ нашего range_final_,
      {                                                                       // (то есть ВЕСЬ вставляемый диапазон находится ПЕРЕД ins_begin)        
        _insert_between_at(ins_begin, range_begin_, range_final_);            // мы можем вставиться в эту позицию, с учетом что при смежнести диапазонов должно быть слияние
      }
      else
      {        
        int pos_final = _search(range_final_, ins_begin);                     // иначе ищем точку вставки для range_final_ начиная с ins_begin
        //
        if (pos_final >= 0)                                                           // здесь у нас начальная точка где-то перед диапазоном ins_begin
        {                                                                             // ... а конечная попала в какой-то валидный диапазон                    
          _insert_overlap_begin(ins_begin, range_begin_, m_data[pos_final].char_final, pos_final);
        }
        else                                                                          // pos_begin < 0 && pos_final < 0 
        {                                                                             //    => и начальная и конечная точка попали где-то между диапазонами
          if (pos_begin == pos_final)                                                 // если это один и тот-же промежуток
            _insert_between_at(ins_begin, range_begin_, range_final_);                // просто вставляемся сюда (_insert_between_at учитывает возможность слияния)
          else
          {
            int ins_final = ~pos_final;
            if (ins_final == m_size)                                                        // ins_final за концом всех наших диапазонов. 
              _insert_overlap_begin(ins_begin, range_begin_, range_final_, ins_final - 1);  // это означает что вставляемый диапазон накрывает все наши диапазоны до конца
            else
              _insert_overlap(ins_begin, ins_final, range_begin_, range_final_);            // и начало и конец вставки в промежутках между диапазонов (т.е. "накрывают" наши диапазоны полностью)
          }            
        }        
      }
      return true;
    }
  }




  // здесь ins_begin_ это валидный диапазон, который БОЛЬШЕ range_begin_
  // мы растягиваем начало диапазона ins_begin вверх до range_begin_
  // ...(возможно слияние с предшествующим ins_begin диапазоном и insertion_pos тогда будет ins_begin-1)
  // и конец результирующего insertion_pos устанавливаем в pos_final.char_final
  // а все диапазоны после insertion_pos + 1 до pos_final включительно удаляем
  void _insert_overlap_begin(size_t ins_begin_, char_t range_begin_, char_t new_range_final_, size_t to_remove_ins_final_)
  {
    _make_insertion_begin(out size_t insertion_pos, ins_begin_, range_begin_);
    m_data[insertion_pos].assign_final(new_range_final_);
    _remove_ranges(insertion_pos + 1, to_remove_ins_final_);
  }


  void _make_insertion_begin(out size_t out_insertion_pos_, size_t insertion_begin_, char_t range_begin_)
  {
    if (insertion_begin_ > 0 && m_data[insertion_begin_ - 1].can_merge_final_with(range_begin_))
      out_insertion_pos_ = insertion_begin_ - 1; //    здесь начало результирующего диапазона [insertion_begin_ - 1].char_begin, no replace begin
    else
    {
      out_insertion_pos_ = insertion_begin_;     // А  здесь начало результирующего диапазона range_begin_
      m_data[out_insertion_pos_].assign_begin(range_begin_);
    }
  }
  void _insert_auto_end(char_t range_begin_, char_t range_final_)
  {
    Debug.Assert(m_size > 0);
    //
    if (m_data[m_size - 1].can_merge_begin_with(range_begin_))
      m_data[m_size - 1].assign_final(range_final_);
    else
      _add(_new_range(range_begin_, range_final_));
  }
  void _insert_overlap(size_t insertion_begin_, size_t insertion_final_, char_t range_begin_, char_t range_final_)
  {
    // обе позиции вставки валидны (не end)
    Debug.Assert(insertion_begin_ >= 0 && insertion_begin_ < m_size);
    Debug.Assert(insertion_final_ >= 0 && insertion_final_ < m_size);
    // и отличаются
    Debug.Assert(insertion_final_ > insertion_begin_);
    // позиция insertion_begin_ ПОСЛЕ НАЧАЛА вставляемого диапазона
    // (но возможно что range_begin_ может слить insertion_begin_ с предыдущим ему)
    Debug.Assert(m_data[insertion_begin_].char_begin > range_begin_);
    // позиция insertion_final_ ПОСЛЕ КОНЦА вставляемого диапазона
    // (но возможно слияние если m_data[insertion_final_].char_begin сразу песле range_final_)
    Debug.Assert(m_data[insertion_final_].char_begin > range_final_);

    // поскольку вставляемый диапазон охватывает как минимум наш один диапазон
    // то собственно вставка диапазона не нужна.
    // нам нужно только отредактировать границы какого-либо существующего, а оставшиеся лишние удалить
    _make_insertion_begin(out size_t insertion_pos, insertion_begin_, range_begin_);
    //
    if (m_data[insertion_final_].can_merge_begin_with(range_final_))
    {
      m_data[insertion_pos].assign_final(m_data[insertion_final_].char_final);
      _remove_ranges_auto(insertion_pos + 1, insertion_final_);
    }
    else
    {     
      m_data[insertion_pos].assign_final(range_final_);
      _remove_ranges_auto(insertion_pos + 1, insertion_final_ - 1);
    }          
  }

  void _insert_between_at(size_t insertion_next_, char_t range_begin_, char_t range_final_)
  {
    // insertion_next_ валиден (не енд)
    Debug.Assert(insertion_next_ >= 0 && insertion_next_ < m_size);
    // и он находится ПОСЛЕ ВСЕГО вставляемого диапазона
    Debug.Assert( m_data[insertion_next_].char_begin > range_final_);
    //
    if (insertion_next_ == 0)
    {
      if (m_data[insertion_next_].is_just_after(range_final_))
        m_data[insertion_next_].assign_begin(range_begin_);
      else
        _insert_at(insertion_next_, _new_range(range_begin_, range_final_));
    }
    else
    { 
      size_t insertion_prev   = insertion_next_ - 1;
      bool b_merge_with_prev  = m_data[insertion_prev].is_just_before(range_begin_);
      bool b_merge_with_next  = m_data[insertion_next_].is_just_after(range_final_);
      if (b_merge_with_prev && b_merge_with_next)
      {
        m_data[insertion_prev].assign_final(m_data[insertion_next_].char_final); // insertion_prev растягиваем до конца insertion_next_
        _remove_range(insertion_next_);
      }
      else if (b_merge_with_prev)
        m_data[insertion_prev].assign_final(range_final_);
      else if (b_merge_with_next)
        m_data[insertion_next_].assign_begin(range_begin_);
      else
        _insert_at(insertion_next_, _new_range(range_begin_, range_final_));
    }    
  }

  

  public bool SetUnionWith(CharsRangeList other_)
  {
    //TODO  это нехороший алгоритм - для каждого Range мы начинаем с начала списка, хотя каждый очередной диапазон нужно добавлять начиная с предыдущего добавленного
    //      именно с того, который перед этим добавили, т.к. добавление идет по алгоритму поглощения и не факт, что диапазон был вообще добавлен, но точка вставки была найдена
    //      хм... а если диапазон был реально добавлен, то начинать нужно именно со следующего.. подумать..
    if (other_.Count > 0)
    {
      bool b_union = this.AddRange(other_.m_data[0].char_begin, other_.m_data[0].char_final);
      for (int i = 1; i < other_.Count; i++)
        b_union |= this.AddRange(other_.m_data[i].char_begin, other_.m_data[i].char_final);
      return b_union;
    }
    return false;
  }


  public void AddSequentionalMove(CharsRangeList other_)
  {
    //TODO  Здесь вызывающий гарантирует, что other_ начинается именно ДАЛЬШЕ нашего конца
    //      (если other_ хоть на одну последнюю чар пересекается с нами, то это уже Объединение, а не Добавление)
    //      у нас возможно расширение последнего диапазона, а оставшиеся диапазоны просто переместить к себе в конец

    Debug.Assert(!ReferenceEquals(this, other_));
    if (ReferenceEquals(this, other_))
      return;

    if (other_.Count == 0)
      return;

    if (m_size == 0)
    {
      m_data = other_.m_data;
      other_.m_data = new range_list_data_t();  //TODO  опять хочется сделать НУЛЛ или какой empty-list придумать..      
      return;
    }

    //TODO  Здесь мы собственно добавляем и нужен throw 
    Debug.Assert(other_.m_data[0].char_begin > this.m_data[m_size - 1].char_final);
    if (!(other_.m_data[0].char_begin > this.m_data[m_size - 1].char_final))
      throw new ArgumentException();

    m_data.EnsureCapacity(m_size + other_.m_size);

    int i_1st = 0;
    if (other_.m_data[0].char_begin == this.m_data[m_size - 1].char_final + 1)
    {
      this.m_data[m_size - 1].assign_final(other_.m_data[0].char_final);
      i_1st = 1;
    }

    for (int i = i_1st; i < other_.Count; ++i)
      m_data.Add(other_.m_data[i]);

    other_.m_data = new range_list_data_t();    //TODO  думается так проще все данные другого в небытие (ГЦ) отправить, чем .Clear() делать
  }


  public void AddSequentionalCopy(CharsRangeList other_)
  {
    //TODO  Здесь вызывающий гарантирует, что other_ начинается именно ДАЛЬШЕ нашего конца
    //      (если other_ хоть на одну последнюю чар пересекается с нами, то это уже Объединение, а не Добавление)
    //      у нас возможно расширение последнего диапазона, а оставшиеся диапазоны просто переместить к себе в конец

    Debug.Assert(!ReferenceEquals(this, other_));
    if (ReferenceEquals(this, other_))
      return;

    if (other_.Count == 0)
      return;

    if (m_size == 0)
    {
      this.m_data = new range_list_data_t(other_.m_data); 
      return;
    }
    
    Debug.Assert(other_.m_data[0].char_begin > this.m_data[m_size - 1].char_final);
    if (!(other_.m_data[0].char_begin > this.m_data[m_size - 1].char_final))
      throw new ArgumentException("AddSequentionalRangeUnsafe argument not sequentional");

    m_data.EnsureCapacity(m_size + other_.m_size);

    int i_1st = 0;
    if (this.m_data[m_size - 1].can_merge_final_with(other_.m_data[0].char_begin))
    {
      this.m_data[m_size - 1].assign_final(other_.m_data[0].char_final);
      i_1st = 1;
    }

    for (int i = i_1st; i < other_.Count; ++i)
      m_data.Add(other_.m_data[i]);
  }
  public static CharsRangeList CombineSequentionalMove(CharsRangeList a_ranges_list_, CharsRangeList b_ranges_list_)
  {
    // вызывающему оба ranges_list_ ненужны. a_ranges_list_ и b_ranges_list_ последовательны
    //TODO  можно подумать надо оптимизацией
    a_ranges_list_.AddSequentionalCopy(b_ranges_list_);
    return a_ranges_list_;
  }



  public bool GetEnumeratorByChar1st(out EnumeratorDataWithTag out_enumerator_data_, ref char_t out_char_)
  {
    if (m_size == 0)
    {
      out_enumerator_data_.tag = default;
      out_enumerator_data_.index = int.MinValue;
      return false;
    }
      
    
    out_enumerator_data_.tag = 0;
    out_enumerator_data_.index = 0;
    out_char_ = m_data[0].char_begin;
    return true;
  }
  public bool GetEnumeratorByCharNext(ref EnumeratorDataWithTag in_out_enumerator_data_, ref char_t out_char_)
  {
    Debug.Assert(m_size > 0);
    Debug.Assert(in_out_enumerator_data_.tag >= 0 && in_out_enumerator_data_.tag < m_size);
    Debug.Assert(in_out_enumerator_data_.index >= 0);

    if (m_data[in_out_enumerator_data_.tag].char_begin + in_out_enumerator_data_.index < m_data[in_out_enumerator_data_.tag].char_final)
    {
      ++in_out_enumerator_data_.index;
      out_char_ = m_data[in_out_enumerator_data_.tag].char_begin + in_out_enumerator_data_.index;
      return true;
    }
    else if (in_out_enumerator_data_.tag < m_size - 1)
    {
      ++in_out_enumerator_data_.tag;
      in_out_enumerator_data_.index = 0;
      out_char_ = m_data[in_out_enumerator_data_.tag].char_begin + in_out_enumerator_data_.index;
      return true;
    }
    else
      return false;
  }

}
