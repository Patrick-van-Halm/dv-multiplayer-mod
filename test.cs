using System;
using System.Collections.Generic;

namespace App {
class Program {
static int runningSum;
static int runningCount;

static void Main() {
bool running = true;

do {
Console.WriteLine("Skriv et heltal for at tilføje til summen eller slut for at få resultat.");
Console.Write("> ");
string input = Console.ReadLine().ToLower();
if(input == "slut") {
Console.WriteLine("Count: {0} Sum: {1}", runningCount, runningSum);
running = false;
}
if(Int32.TryParse(input, out int n)) {
runningSum += n;
runningCount++;
continue;
}
Console.WriteLine("Input ikke forstået. Skriv et heltal eller \"slut\".");
} while(running);
}
}
}

