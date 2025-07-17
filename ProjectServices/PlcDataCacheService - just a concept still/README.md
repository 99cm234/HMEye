# PlcDataCacheService
name sucks. Need better.

## TODO:
1. Write queue overload: what happens when write queue accumulate beyond the set number.

2. Cache Invalidation. 
	- Non listed reads cache indefinitely and later reads will return a stale result.
	- Consider timestamp and invalidation or re-read strategy.

3. Error state does not persist
	- are transient errors confusing?
	- Consider error count and polling disable until external reset

4. Hardcoded list of monitored variables is gross. 
	- Pass in List via parameter?
	- Config file?
	- API?

5. Write method
	- should writes wait for success to cache
	- should cached value be checked before writing to avoid needless re-writes?