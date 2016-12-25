import sys

def Computation(input):
    return input

if __name__ == '__main__':
    while True:
        line = sys.stdin.readline()
        if not line: break # EOF
        sys.stdout.write(line)

