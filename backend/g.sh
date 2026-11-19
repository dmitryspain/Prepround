u=$1; shift
curl -sL -A 'Mozilla/5.0' "$u" | sed 's/<[^>]*>//g' > p.txt; echo "== $u $(wc -c <p.txt)"
for k in "$@"; do echo "-- $k"; grep -i -m2 -C1 "$k" p.txt | cut -c1-500; done
