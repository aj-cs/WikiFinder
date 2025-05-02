using System.Collections;
using System.Text;
using SearchEngine.Core.Interfaces;

namespace SearchEngine.Core;

public class BloomFilter : IBloomFilter  
{  
    private readonly BitArray _bits;  
    private readonly int _hashCount;  

    public BloomFilter(int expectedItems, double falsePositiveRate)  
    {  
        if (expectedItems <= 0) throw new ArgumentOutOfRangeException(nameof(expectedItems));
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1)
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));

        // compute bit-array size (m) and hash count (k)
        // m↑ -> decreases false positive rate but increases memory usage
        // optimal m given by m = -(n * ln(p)) / (ln(2)^2)
        var m = (int)Math.Ceiling(-expectedItems * Math.Log(falsePositiveRate)
                                  / (Math.Log(2) * Math.Log(2)));
        // k↑ -> decreases false positive rate (to an optimum then ↑) but increases computation time
        // optimal k given by k = (m / n) * ln(2)
        var k = (int)Math.Round((m / (double)expectedItems) * Math.Log(2));

        _bits = new BitArray(m);
        _hashCount = k;
    }  

    public void Add(string term)  
    {  
        var bytes = Encoding.UTF8.GetBytes(term);  
        for (uint i = 0; i < _hashCount; i++)  
        {  
            uint hash = MurmurHash3(bytes, i);  
            _bits[(int)(hash % _bits.Length)] = true;  
        }  
    }  

    public bool MightContain(string term)  
    {  
        var bytes = Encoding.UTF8.GetBytes(term);  
        for (uint i = 0; i < _hashCount; i++)  
        {  
            uint hash = MurmurHash3(bytes, i);  
            if (!_bits[(int)(hash % _bits.Length)]) return false;  
        }  
        return true;  
    }
    // MurmurHash3 ported from Austin Appleby's C++ implementation
    // // https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
    private static uint MurmurHash3(byte[] data, uint seed)
    {
        const uint c1 = 0xcc9e2d51, c2 = 0x1b873593;
        uint h1 = seed;
        int len = data.Length, roundedEnd = len & ~3;
        for (int i = 0; i < roundedEnd; i += 4)
        {
            uint k1 = (uint)(data[i] | data[i+1]<<8 | data[i+2]<<16 | data[i+3]<<24);
            k1 = Rotl32(k1 * c1, 15) * c2;
            h1 = Rotl32(h1 ^ k1, 13) * 5 + 0xe6546b64;
        }
        uint k2 = 0;
        switch (len & 3)
        {
            case 3: k2 ^= (uint)data[roundedEnd+2] << 16; goto case 2;
            case 2: k2 ^= (uint)data[roundedEnd+1] << 8; goto case 1;
            case 1:
                k2 ^= data[roundedEnd];
                k2 = Rotl32(k2 * c1, 15) * c2;
                h1 ^= k2;
                break;
        }
        h1 ^= (uint)len;
        return FMix(h1);
    }

    private static uint Rotl32(uint x, byte r) => (x << r) | (x >> (32 - r));
    private static uint FMix(uint h)
    {
        h ^= h >> 16; h *= 0x85ebca6b;
        h ^= h >> 13; h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    } 
}  