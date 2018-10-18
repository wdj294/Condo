/*
  Created by
  Juan Sebastian Munoz Arango
  naruse@gmail.com
  all rights reserved
 */

namespace ProDrawCall {
    using System;
    using System.Collections.Generic;

    public sealed class Tuple<T1, T2> {
        private readonly T1 item1;
        public T1 Item1 { get { return item1; } }

        private readonly T2 item2;
        public T2 Item2 { get { return item2; } }

        /// <summary>
        /// Create a new tuple value
        /// </summary>
        /// <param name="item1">First element of the tuple</param>
        /// <param name="second">Second element of the tuple</param>
        public Tuple(T1 item1, T2 item2) {
            this.item1 = item1;
            this.item2 = item2;
        }

        public override string ToString() {
            return string.Format("Tuple({0}, {1})", Item1, Item2);
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + item1.GetHashCode();
            hash = hash * 23 + item2.GetHashCode();
            return hash;
        }

        public override bool Equals(object o) {
            if (o.GetType() != typeof(Tuple<T1, T2>)) {
                return false;
            }
            var other = (Tuple<T1, T2>) o;
            return this == other;
        }

        public static bool operator == (Tuple<T1, T2> a, Tuple<T1, T2> b) {
            if(object.ReferenceEquals(a,null) && object.ReferenceEquals(b, null))//both null
                return true;
            if((object.ReferenceEquals(a,null) && !object.ReferenceEquals(b, null)) || (object.ReferenceEquals(b,null) && !object.ReferenceEquals(a, null)))//one null and not the other
                return false;
            //TODO: implement when tuple contents (Item1 / Item2) are null.
            return  a.item1.Equals(b.item1) && a.item2.Equals(b.item2);
        }

            public static bool operator != (Tuple<T1, T2> a, Tuple<T1, T2> b) {
                return !(a == b);
            }
    }
}