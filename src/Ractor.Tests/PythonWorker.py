import sys
import redis
import uuid
import time
import json

id = "PythonEcho"
group = ""
max_concurrency = 1
timeout = 60.0

def Computation(input):
    return int(input) * 2

#if __name__ == '__main__':
connection = "localhost" # sys.argv[1]
namespace = "R:" #sys.argv[2]

prefix = namespace + (("{" + group + "}:") if group else "") + id + ":"
inbox = prefix + "inbox"
#print(inbox)
pipeline = prefix + "pipeline"
channelKey = "__keyspace@0__:" + inbox
resultKey = prefix + "asyncdictionary:"

lua = """
        local result = redis.call('RPOP', KEYS[1])
        if result ~= nil then
            redis.call('HSET', KEYS[2], KEYS[3], result)
        end
        return result"""

def notify(m = None):
    try:
        if (m['data'] == 'lpush' or  m['data'] == 'rpush') and notification_semaphore.counter == 0:
            #print("received notification") #  + str(m))
            notification_semaphore.release()
    except:
        pass

r = redis.StrictRedis(connection)
receive = r.register_script(lua)

def process():
    while True:
        pipelineId = str(uuid.uuid4())
        message = receive(keys = [inbox, pipeline, pipelineId])
        if message:
            #print(message)
            decoded = json.loads(message.decode('utf-8', 'replace'))
            if decoded['p']['h']:
                #print("has error")
                pass
            else:
                try:
                    decoded['p']['v'] = Computation(decoded['p']['v'])
                except Exception as e:
                    decoded['p']['v'] = Nil
                    decoded['p']['h'] = True
                    decoded['p']['e'] = str(e) #''.join(traceback.format_exception( *sys.exc_info())[-2:]).strip().replace('\n',': ')
            encoded = json.dumps(decoded['p'])
            #print(resultKey)
            r.set(resultKey + decoded['i'], encoded)
            r.hdel(pipeline, pipelineId)
            #print(encoded)
        else:
            #print("No messages " + str(getcurrent()))
            time.sleep(1)
    
process()
        

        
        

