#!/bin/bash
set -e
# Ensure proxy include directories and patch vhosts if needed
for site in litecashier-keys.smartstick-iq.com litecashier.smartstick-iq.com restaurant.smartstick-iq.com; do
  mkdir -p /www/server/panel/vhost/nginx/proxy/$site
  mkdir -p /www/server/panel/vhost/nginx/extension/$site
done

cp /tmp/keys.proxy.conf /www/server/panel/vhost/nginx/proxy/litecashier-keys.smartstick-iq.com/proxy.conf
cp /tmp/cashier.proxy.conf /www/server/panel/vhost/nginx/proxy/litecashier.smartstick-iq.com/proxy.conf
cp /tmp/restaurant.proxy.conf /www/server/panel/vhost/nginx/proxy/restaurant.smartstick-iq.com/proxy.conf

# Patch main conf to include proxy (like pos-api) if missing
patch_vhost() {
  local f="/www/server/panel/vhost/nginx/$1.conf"
  local inc="include /www/server/panel/vhost/nginx/proxy/$1/*.conf;"
  if ! grep -q "vhost/nginx/proxy/$1" "$f"; then
    # insert before PHP-INFO-START
    sed -i "s|#PHP-INFO-START.*|#PHP-INFO-START  PHP reference configuration, allowed to be commented, deleted or modified\n\t$inc\n\tinclude enable-php-00.conf;\n    #PHP-INFO-SKIP|" "$f"
    # remove old enable-php-83 if we replaced awkwardly - do carefully
  fi
}

for site in litecashier-keys.smartstick-iq.com litecashier.smartstick-iq.com restaurant.smartstick-iq.com; do
  f="/www/server/panel/vhost/nginx/${site}.conf"
  if ! grep -q "vhost/nginx/proxy/${site}" "$f"; then
    # Comment PHP 83 and add proxy include after SSL-END block area - insert after REWRITE-END
    sed -i "/#REWRITE-END/a\\\n    # LiteCashier reverse proxy (docker)\n    include /www/server/panel/vhost/nginx/proxy/${site}/*.conf;\n" "$f"
    # Disable PHP handler conflict by switching to enable-php-00
    sed -i 's/include enable-php-83.conf;/include enable-php-00.conf;/' "$f"
  fi
done

nginx -t && nginx -s reload
echo NGINX_OK
