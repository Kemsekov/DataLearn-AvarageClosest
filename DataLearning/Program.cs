//Some data learning samples
// Examples.Example1();
// Examples.Example2();
// Examples.Example3();    
// Examples.Example4();
// 
//playground

void FillWithRandom(Vector vec){
    var r = Random.Shared;
    for(int i = 0;i<vec.Count;i++){
        vec[i] = r.NextSingle();
    }
}

var dataStorage = new DataStorage<float>(5,5);
var vec1 = new ArrayedVector(dataStorage);
var vec2 = new ArrayedVector(dataStorage);
var vec3 = new ArrayedVector(dataStorage);
FillWithRandom(vec1);
FillWithRandom(vec2);
FillWithRandom(vec3);
foreach(var a in dataStorage.Storage.Chunk(5)){
    foreach(var b in a)
        System.Console.Write($"{b} ");
    System.Console.WriteLine();
}

// Playground.Run(args);