module lwsharp.SemanticComposition

type Monoid<'a> =
    { Empty : 'a
      Append : 'a -> 'a -> 'a }

let listMonoid<'a> = {
      Empty = []
      Append = (@)
    }
