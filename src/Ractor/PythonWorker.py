﻿import sys
import redis
import uuid
import gevent
from gevent import getcurrent
from gevent.lock import BoundedSemaphore
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
concurrency_semaphore = BoundedSemaphore(max_concurrency)
notification_semaphore = BoundedSemaphore(1)

def release():
    #print("received notification")
    try:
        if concurrency_semaphore.counter == 0:
            concurrency_semaphore.release()
            #print("released semaphore")
    except:
        pass

def notify(m = None):
    try:
        if (m['data'] == 'lpush' or  m['data'] == 'rpush') and notification_semaphore.counter == 0:
            #print("received notification") #  + str(m))
            notification_semaphore.release()
    except:
        pass

r = redis.StrictRedis(connection)
p = r.pubsub()
p.subscribe(**{channelKey: notify })
listener_thread = p.run_in_thread(sleep_time=0.001)
#retry_thread = threading.Timer(1.0, release).start()
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
            release()
            gevent.sleep(0)
            break #finish loop
        else:
            #print("No messages " + str(getcurrent()))
            notification_semaphore.acquire(timeout = 0.1)
            gevent.sleep(0)
    
while True:
    if(not concurrency_semaphore.acquire(timeout = 0.1)):
         #print("cannot acquire semaphore")
        pass
    else:
        #print("acquired concurrency semaphore")
        gevent.Greenlet.spawn(process)
        

        
        

