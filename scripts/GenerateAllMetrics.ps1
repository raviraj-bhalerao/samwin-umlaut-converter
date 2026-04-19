Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\CacheHit-MissRequestsGenerator.ps1
.\CacheMissRequestsGenerator.ps1
.\LoginRequestsGenerator.ps1
.\MixedLoadRequestsGenerator.ps1
.\RateLimiterRequestsGenerator.ps1