
namespace Ractor {

    // For Ractor actor use FsPickler since messages are DUs (payloads are POCOs though)

    public interface ISerializer {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] bytes);
        T DeepClone<T>(T value);
    }
    
}
