.comp DiGraph<N> {
    .decl node(a:N)
    .decl edge(a:N,b:N) overridable
    .decl start(i : N)

    node(X) :- edge(X,_).
    node(X) :- edge(_,X).

    .decl reach(a:N,b:N)
    reach(X,Y) :- edge(X,Y).
    reach(X,Z) :- reach(X,Y),reach(Y,Z).

    // Non-dominance of input graph
    //
    .decl non_dom(s:N,d:N)
    non_dom(x,y) :- start(x), node(y), x!=y.
    non_dom(x,y) :- non_dom(z,y), edge(z,x), !start(x), x!=y .

    //
    // Dominance of input graph
    //
    .decl dom(s:N,d:N)
    dom(x,y) :- node(x), node(y), !non_dom(x,y).

    //
    // Finds nodes that strictly dominate a node
    //
    .decl s_dom(n:N,d:N)
    s_dom(n,d) :- dom(n,d), n != d.

    //
    // Counts the size strictly dominating set
    //
    .decl s_domsize(n:N,s:number)
    s_domsize(n,0) :- start(n).
    s_domsize(n,s) :- s_dom(n,_), s = count : s_dom(n,_).

    //
    // Set of sizes of strictly dominating nodes
    //
    .decl s_domsizes(n:N,d:N,m:number)
    s_domsizes(n,d,s) :- s_dom(n,d), s_domsize(d,s).

    //
    // Max size of strictly dominating nodes.
    //
    .decl s_max_domsize(n:N,s:number)
    s_max_domsize(n,0) :- start(n).
    s_max_domsize(n,y) :- s_domsizes(n,_,_), y = max s : s_domsizes(n,_,s).

    //
    // Immediate Dominator
    //
    .decl imdom(n:N,i:N)
    imdom(n,x) :- s_dom(n,x), s_domsize(x,s), s_max_domsize(n,s).

    .decl dom_frontier(n:N,i:N)
    dom_frontier(x,y) :- node(z), edge(z, y), dom(z, x), !s_dom(y, x).
}

.type JumpInstruction <: symbol
.type BasicInstruction <: symbol

.type Instruction = JumpInstruction | BasicInstruction

.type Variable <: symbol

.comp InstructionSequence  : DiGraph<Instruction> {
    .decl var_def(v : Variable , m : Instruction)

    // b is a's successor
    .decl successor(a:Instruction, b:Instruction)
    // if an exception is thrown while executing a, b is a's successor
    .decl exceptional_successor(a:Instruction, b:Instruction)

    .override edge
    edge(x, y) :- successor(x, y).
    edge(x, y) :- exceptional_successor(x, y).
    // Depending on the sort of analysis that is performed,
    // control flow may be thought in this way. For phi nodes calculation it should not
    // be necessary.
    // edge(x, y) :- node(x), node(y), node(z), edge(x, z), exceptional_successor(z, y).

    .decl phi_location(v : Variable , m : Instruction)
    phi_location(v, i) :- var_def(v, j), dom_frontier(j, i), edge(x, i), edge(y, i), x!=y.
    phi_location(v, i) :- phi_location(v, n), dom_frontier(n, i), edge(x, i), edge(y, i), x!=y.
}