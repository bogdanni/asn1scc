﻿module RangeSets0

#if false

type V_CMP =
    | V_LT
    | V_EQ
    | V_GT




type LPoint<'v when 'v : comparison> =
    | LP of ('v*bool)

type RPoint<'v when 'v : comparison> =
    | RP of ('v*bool)

    
[<CustomEquality; CustomComparison>]
type Point<'v when 'v : comparison>  = 
    | LPoint of LPoint<'v>
    | RPoint of RPoint<'v>
with
    static member v_comp (p1: Point<'v>) (p2 :Point<'v>) =
        match p1, p2 with
        | LPoint(LP(a, a_inc)), LPoint(LP(b, b_inc)) when a = b && a_inc = b_inc -> V_EQ
        | LPoint(LP(a, a_inc)), LPoint(LP(b, b_inc)) when a = b && not a_inc && b_inc -> V_LT
        | LPoint(LP(a, a_inc)), LPoint(LP(b, b_inc)) when a = b && a_inc && not b_inc -> V_GT
        | LPoint(LP(a, a_inc)), LPoint(LP(b, b_inc))    -> if a<b then V_LT else V_GT
        | LPoint(LP(a,_)), RPoint(RP(b,_))  when a=b    -> V_EQ
        | LPoint(LP(a,_)), RPoint(RP(b,_))              -> if a<b then V_LT else V_GT
        | RPoint(RP(a,_)), RPoint(RP(b,_))  when a=b    -> V_EQ
        | RPoint(RP(a,_)), LPoint(LP(b,_))              -> if a<b then V_LT else V_GT
        | RPoint(RP(a, a_inc)), RPoint(RP(b, b_inc)) when a = b && a_inc = b_inc -> V_EQ
        | RPoint(RP(a, a_inc)), RPoint(RP(b, b_inc)) when a = b && not a_inc && b_inc -> V_LT
        | RPoint(RP(a, a_inc)), RPoint(RP(b, b_inc)) when a = b && a_inc && not b_inc -> V_GT
        | RPoint(RP(a, a_inc)), RPoint(RP(b, b_inc))    -> if a<b then V_LT else V_GT
    
    static member comp p1  p2  =
        match Point<'v>.v_comp p1 p2 with
        | V_LT  -> -1
        | V_EQ  -> 0
        | V_GT  -> 1

    override this.Equals(yobj) =
        match yobj with
        | :? Point<'v> as other -> Point<'v>.comp this other = 0
        | _ -> false
    override this.GetHashCode() = 
        match this with
        | LPoint(LP(a, _))   -> a.GetHashCode()
        | RPoint(RP(a, _))   -> a.GetHashCode()

    interface System.IComparable with 
        member this.CompareTo oth = 
            match oth with
            | :? Point<'v> as other -> Point<'v>.comp this other
            | _     -> invalidArg "oth" "cannot compare values of different types"


type OneOrTwo<'T> =
    | One of 'T
    | Two of 'T*'T
with 
    member this.toList =
        match this with
        | One a     -> [a]
        | Two (a,b) -> [a;b]


type Range<'v when 'v : comparison> =
    | Range_Empty
    | Range_Universe
    | Range_NI_A of RPoint<'v>
    | Range_B_PI of LPoint<'v>
    | Range_AB of LPoint<'v>*RPoint<'v>
with
    member this.isWithin (p:Point<'v>) =
        match this with
        |Range_Empty                          -> false
        |Range_Universe                       -> true
        |Range_NI_A a                         -> p <= RPoint a 
        |Range_B_PI b                         -> LPoint b <= p
        |Range_AB  (a,b)                      -> LPoint a <= p && p <= RPoint b

    member this.intersect (other:Range<'v>) =
        match this, other with
        | Range_Empty, _                                -> Range_Empty
        | _, Range_Empty                                -> Range_Empty
        | Range_Universe, _                             -> other
        | _, Range_Universe                             -> this
        | Range_NI_A (RP(a1, inc1)), Range_NI_A (RP(a2, inc2))  -> Range_NI_A(RP (min a1 a2, (if a1 < a2 then inc1 else inc2)))
        | (Range_NI_A a), (Range_B_PI b )  -> 
            // case 1 : OO ---- a   b --------- OO
            // case 2 : OO ---- a   
            //                  b --------- OO
            // case 3 : OO ---- a   
            //               b --------- OO
             match Point<'v>.v_comp (RPoint a) (LPoint b) with
             | V_LT      -> Range_Empty             
             | V_EQ      -> Range_AB(b, a)
             | V_GT      -> Range_AB(b, a)
        | Range_NI_A x0, Range_AB (a, b)   ->
            // -oo --------x0
            //         a--------b
            match Point<'v>.v_comp (RPoint x0) (LPoint a) with
            | V_LT      -> Range_Empty
            | V_EQ      -> Range_AB (a, x0)
            | V_GT      -> 
                match Point<'v>.v_comp (RPoint x0) (RPoint b) with
                | V_LT  -> Range_AB(a, x0)
                | V_EQ  -> Range_AB(a, x0)
                | V_GT  -> other
        | Range_B_PI _, Range_NI_A _      -> other.intersect this
        | Range_B_PI (LP (b1, inc1)), Range_B_PI(LP (b2, inc2))       -> Range_B_PI(LP (max b1 b2, (if b1 > b2 then inc1 else inc2)) )
        | Range_B_PI x0,  Range_AB (a, b)   ->
            //      x0 -------------- +oo
            //   a-------b         
            match Point<'v>.v_comp (RPoint b) (LPoint x0) with
            | V_LT      -> Range_Empty
            | V_EQ      -> Range_AB( x0, b)
            | V_GT      ->
                match Point<'v>.v_comp (LPoint a) (LPoint x0) with
                | V_GT -> other
                | V_EQ 
                | V_LT -> Range_AB (x0, b)
        | Range_AB _, Range_NI_A _      -> other.intersect this
        | Range_AB _, Range_B_PI _      -> other.intersect this
        | Range_AB (A1,A2), Range_AB (B1,B2) -> 
            let a1 = LPoint A1
            let b1 = LPoint B1
            let a2 = RPoint A2
            let b2 = RPoint B2
              // case 1 :    a1------a2             , condition a2 < b1,         --> empty set
              //                         b1-----b2 
              // case 2 :    a1------a2
              //                     b1------b2     , condition a2 = b1,         --> [a2,a2]
              // case 3 :    a1------a2
              //                  b1------b2        , condition a1 <= b1 && a2 <= b2,          --> [b1,a2]
              // case 4 :    a1-------------a2
              //                  b1------b2        , condition a1 <= b1 && b2 <= a2,          --> other
              // case 5 :       a1-----a2
              //              b1----------b2        , condition b1 <= a1 && a2 <= b2,          --> this
              
              // case 6 :       a1--------a2
              //              b1--------b2          , condition b1 <= a1 && b2 <= a2,          --> [a1,v2]
              
              // case 7 :               a1--------a2
              //              b1--------b2          , condition a1 = b2 ,                      --> [a1,a1]
              
              // case 8 :               a1--------a2
              //              b1-----b2             , condition b2 < a1,                       --> empty set

            if   a2 < b1 then Range_Empty
            elif a2 = b1 then Range_AB (B1,A2)
            elif a1 <= b1 && a2 <= b2 then Range_AB (B1,A2)
            elif a1 <= b1 && b2 <= a2 then other
            elif b1 <= a1 && a2 <= b2 then this
            elif b1 <= a1 && b2 <= a2 then Range_AB(A1,B2)
            elif a1 = b2 then Range_AB(A1,B2)
            else Range_Empty

    member this.union (other:Range<'v>) =
        match this, other with
        | Range_Empty, _                                -> One other
        | _, Range_Empty                                -> One this
        | Range_Universe, _                             -> One Range_Universe
        | _, Range_Universe                             -> One Range_Universe
        | Range_NI_A (RP(a1, inc1)), Range_NI_A (RP(a2, inc2))  -> One (Range_NI_A(RP (max a1 a2, (if a1 > a2 then inc1 else inc2))))
        | (Range_NI_A a), (Range_B_PI b )  -> 
            // case 1 : OO ---- a   b --------- OO
            // case 2 : OO ---- a   
            //                  b --------- OO
            // case 3 : OO ---- a   
            //               b --------- OO
             match Point<'v>.v_comp (RPoint a) (LPoint b) with
             | V_LT      -> Two (this, other)
             | V_EQ      -> One Range_Universe
             | V_GT      -> One Range_Universe
        | Range_NI_A x0, Range_AB (a, b)   ->
            // -oo --------x0
            //         a--------b
            match Point<'v>.v_comp (RPoint x0) (LPoint a) with
            | V_LT      -> Two (this, other)
            | V_EQ      -> One (Range_NI_A b)
            | V_GT      -> 
                match Point<'v>.v_comp (RPoint x0) (RPoint b) with
                | V_LT  -> One (Range_NI_A b)
                | V_EQ  -> One (Range_NI_A b)
                | V_GT  -> One (this)
        | Range_B_PI _, Range_NI_A _      -> other.union this
        | Range_B_PI (LP (b1, inc1)), Range_B_PI(LP (b2, inc2))       -> One (Range_B_PI(LP (min b1 b2, (if b1 < b2 then inc1 else inc2)) ))
        | Range_B_PI x0,  Range_AB (a, b)   ->
            //      x0 -------------- +oo
            //   a-------b         
            match Point<'v>.v_comp (RPoint b) (LPoint x0) with
            | V_LT      -> Two(this, other)
            | V_EQ      -> One (Range_B_PI  a)
            | V_GT      ->
                match Point<'v>.v_comp (LPoint a) (LPoint x0) with
                | V_GT -> One (Range_B_PI  a)
                | V_EQ 
                | V_LT -> One (this)
        | Range_AB _, Range_NI_A _      -> other.union this
        | Range_AB _, Range_B_PI _      -> other.union this
        | Range_AB (A1,A2), Range_AB (B1,B2) -> 
            let a1 = LPoint A1
            let b1 = LPoint B1
            let a2 = RPoint A2
            let b2 = RPoint B2
              // case 1 :    a1------a2             , condition a2 < b1,         --> empty set
              //                         b1-----b2 
              // case 2 :    a1------a2
              //                     b1------b2     , condition a2 = b1,         --> [a2,a2]
              // case 3 :    a1------a2
              //                  b1------b2        , condition a1 <= b1 && a2 <= b2,          --> [b1,a2]
              // case 4 :    a1-------------a2
              //                  b1------b2        , condition a1 <= b1 && b2 <= a2,          --> other
              // case 5 :       a1-----a2
              //              b1----------b2        , condition b1 <= a1 && a2 <= b2,          --> this
              
              // case 6 :       a1--------a2
              //              b1--------b2          , condition b1 <= a1 && b2 <= a2,          --> [a1,v2]
              
              // case 7 :               a1--------a2
              //              b1--------b2          , condition a1 = b2 ,                      --> [a1,a1]
              
              // case 8 :               a1--------a2
              //              b1-----b2             , condition b2 < a1,                       --> empty set

            if   a2 < b1 then Two(this, other)                             // case 1
            elif a2 = b1 then One (Range_AB (A1,B2))                        // case 2
            elif a1 <= b1 && a2 <= b2 then One (Range_AB (A1,B2))           // case 3
            elif a1 <= b1 && b2 <= a2 then One (this)                       // case 4
            elif b1 <= a1 && a2 <= b2 then One (other)                      // case 5
            elif b1 <= a1 && b2 <= a2 then One (Range_AB(B1,A2))            // case 6
            elif a1 = b2 then One (Range_AB(B1,A2))                         // case 7
            else Two(other, this)                                          // case 8

        member this.isBefore other =
            match this.union other with
            | One _            -> false
            | Two (first, sec) -> first = this && sec = other

        member this.isAfter other =
            match this.union other with
            | One _            -> false
            | Two (first, sec) -> first = other && sec = this
                                                       //  
type RangeSet<'v when 'v : comparison> =
    | Range of Range<'v>
    | RangeCollection of (Range<'v>*Range<'v>*Range<'v> list)
with
    static member createFromRangeList ranges =
        match ranges |> List.exists((=) Range_Universe) with
        | true  -> Range Range_Universe
        | false ->
            match ranges |> List.filter ((<>) Range_Empty) with
            | []     -> Range Range_Empty
            | r1::[] -> Range r1
            | r1::r2::rest -> RangeCollection (r1,r2,rest)
        
    member this.isInSet (vl:Point<'v>) : bool =
        match this with
        | Range(r )                         -> r.isWithin vl 
        | RangeCollection (r1,r2,rest)      -> r1::r2::rest |> List.exists(fun r -> r.isWithin vl )
    member this.intersect (other:RangeSet<'v>) =
        match this, other with
        | Range r1, Range r2                                -> Range (r1.intersect r2)
        | Range r0, RangeCollection (r1,r2,rest)            ->
            let ranges = r1::r2::rest |> List.map(fun r -> r.intersect r0) |> List.filter(fun r -> r <> Range_Empty)
            RangeSet<'v>.createFromRangeList ranges
        | RangeCollection _, Range _                     -> other.intersect this
        | RangeCollection (a1,a2,aRest), RangeCollection _  ->
            let ranges = a1::a2::aRest |> List.map(fun r -> (Range r).intersect other) |> List.collect(fun rs -> match rs with Range r -> [r] | RangeCollection (r1,r2,rRest) -> r1::r2::rRest )
            RangeSet<'v>.createFromRangeList ranges
    member this.complement = 
        match this with
        | Range(Range_Empty )                         -> Range(Range_Universe)
        | Range(Range_Universe)                       -> Range(Range_Empty)
        | Range(Range_NI_A (RP (a,inc)))              -> Range(Range_B_PI(LP (a, not inc)))
        | Range(Range_B_PI (LP (b,inc)))              -> Range(Range_NI_A(RP (b, not inc)))
        | Range(Range_AB  (LP(a,inc1), RP(b,inc2)))   -> RangeCollection ( (Range_NI_A (RP(a, not inc1))), (Range_B_PI(LP(b,not inc2))), [])
        | RangeCollection (r1,r2,rest)                            -> 
            r1::r2::rest |> List.map(fun r -> (Range r).complement) |> List.fold(fun newRange curR -> newRange.intersect curR ) (Range Range_Universe)
    member this.differece (other:RangeSet<'v>) =
        other.complement.intersect this
    
    member this.union (other:RangeSet<'v>) =
        match this, other with
        | Range r1, Range r2                                -> 
            match r1.union r2 with
            | One  r -> Range r
            | Two (r1,r2) -> RangeCollection(r1, r2, [])
        | Range r0, RangeCollection (r1,r2,rest)            ->
            let before = r1::r2::rest |> List.filter(fun r -> r.isBefore r0)
            let after = r1::r2::rest |> List.filter(fun r -> r.isAfter r0)
            let middleRange = 
                r1::r2::rest |> 
                List.fold(fun (nr:Range<'v>) r -> 
                    match nr.union r with
                    | Two _     -> nr   // ingore ranges which are before or after r0
                    | One ur    -> ur) r0
            RangeSet<'v>.createFromRangeList (before@[middleRange]@after)
        | RangeCollection _, Range _                     -> other.intersect this
        | RangeCollection (a1,a2,aRest), RangeCollection _  ->
            a1::a2::aRest |> 
            List.fold(fun (ret:RangeSet<'v>) r -> ret.union (Range r) ) other
            
#endif