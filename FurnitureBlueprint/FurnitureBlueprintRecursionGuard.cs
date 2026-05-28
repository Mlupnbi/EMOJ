namespace EvenMoreOverpoweredJourney.FurnitureBlueprint

{

    /// <summary>防止配方打分 → 锚点判定 → 分类 互相递归导致栈溢出。</summary>

    internal static class FurnitureBlueprintRecursionGuard

    {

        private const int MaxAnchorOrClassifyDepth = 8;



        [System.ThreadStatic]

        private static int _anchorOrClassifyDepth;



        public static bool IsInsideAnchorOrClassify => _anchorOrClassifyDepth > 0;



        public static bool IsDepthExceeded => _anchorOrClassifyDepth >= MaxAnchorOrClassifyDepth;



        public static RecursionScope EnterAnchorOrClassify() => new RecursionScope();



        internal readonly struct RecursionScope : System.IDisposable

        {

            private readonly bool _entered;



            public RecursionScope()

            {

                if (_anchorOrClassifyDepth >= MaxAnchorOrClassifyDepth)

                {

                    _entered = false;

                    return;

                }



                _anchorOrClassifyDepth++;

                _entered = true;

            }



            public bool Entered => _entered;



            public void Dispose()

            {

                if (_entered)

                    _anchorOrClassifyDepth--;

            }

        }

    }

}


