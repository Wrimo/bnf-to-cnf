# bnf-to-cnf
C# program that converts a BNF grammar to CNF

# Usage 
Nonterminals in BNF must be wrapped by *<* and *>* like *\<Expr\>* and *\<Statement\>*. Anything else is treated as a terminal. 

Do not start nonterminals with *$* as it is used to mark generated nonterminals. 

# Example 

BNF input: 
```
<a> ::= <b> | <c> <d> <e>
<b> ::= eat food 
<c> ::= <d> food
<d> ::= yum
<e> ::= <b> <c> <a>
```

CNF Ouput: 
```
<a> -> <c> <$11<a>> | <$eat> <$food> 
<b> -> <$eat> <$food> 
<c> -> <d> <$food> 
<d> -> yum 
<e> -> <b> <$01<e>> 
<$S> -> <c> <$11<a>> | <$eat> <$food> 
<$eat> -> eat 
<$food> -> food 
<$11<a>> -> <d> <e> 
<$01<e>> -> <c> <a> 
```