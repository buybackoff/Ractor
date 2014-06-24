using ServiceStack.Text;

namespace Fredis {

    // For Fredis actor use FsPickler since messages are DUs (payloads are POCOs though)

    public interface ISerializer {
        byte[] Serialize<T>(T value);
        T Deserialize<T>(byte[] bytes);
    }


    
}
