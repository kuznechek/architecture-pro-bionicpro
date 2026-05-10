from fastapi import Request, HTTPException

def verify_user_id(request: Request):
    user_id = request.headers.get("X-User-Id")
    if not user_id:
        raise HTTPException(status_code=401, detail="Missing user identity")
    return user_id

def get_current_user_id(request: Request):
    return verify_user_id(request)