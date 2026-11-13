"""Reviewed-scope diagnostic only: fixed cooperative issuer case, no trace/value output."""
from pathlib import Path
import argparse,hashlib,json,re
import freshcontroller as controller

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--execute',action='store_true')
    parser.add_argument('--pins-sha256',required=True)
    parser.add_argument('--results',required=True)
    parser.add_argument('--attempts',type=int,default=5,choices=range(1,6))
    args=parser.parse_args()
    if not args.execute:
        print(json.dumps({'preparationOnly':True,'case':'profile-owner3-caller2-issuer','maximumAttempts':args.attempts}));return
    assert re.fullmatch('[0-9a-f]{64}',args.pins_sha256)
    prep=controller.PREP;raw=(prep/'preparation-receipt.json').read_bytes()
    assert hashlib.sha256(raw).hexdigest()==args.pins_sha256
    pins=json.loads(raw)
    for name,wanted in pins['filesSha256'].items():
        relative=Path(name);assert not relative.is_absolute()and'..'not in relative.parts
        file=prep/relative;assert file.is_file()and not file.is_symlink()and hashlib.sha256(file.read_bytes()).hexdigest()==wanted
    for field in ['sourceSha256','app147Sha256']:
        for name,wanted in pins[field].items():assert hashlib.sha256((controller.ROOT/name).read_bytes()).hexdigest()==wanted
    assert re.fullmatch('results-[a-z0-9-]{1,64}',args.results)
    root=prep/args.results;root.mkdir(mode=0o700,exist_ok=False)
    complete=0
    try:
        for number in range(1,args.attempts+1):
            attempt=root/f'attempt-{number}';attempt.mkdir(mode=0o700)
            controller.one('disabled','slow','profile-owner3-caller2-issuer',13100+number,attempt,1)
            complete+=1
    finally:
        (root/'diagnostic-summary.json').write_text(json.dumps({'scratchDiagnosticOnly':True,'completedPassingAttempts':complete,'maximumAttempts':args.attempts,'stopsOnFirstFailure':True},indent=2)+'\n')

if __name__=='__main__':main()
