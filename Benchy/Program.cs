// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using Benchy;
using TestBed;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<DatesBenchy>();