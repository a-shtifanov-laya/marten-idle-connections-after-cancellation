import http from 'k6/http';
import { check, sleep } from 'k6';

// k6 test configuration
export const options = {
    vus: 10,             // concurrent virtual users (parallel requests)
    duration: '1m'
};

const tenantCodes = ["1", "2", "3", "10", "11", "12"];
export default function () {
    const tenantCode = tenantCodes[Math.floor(Math.random() * tenantCodes.length)];
    const url = `http://localhost:5197/tenants/${tenantCode}/basket`;

    const params = {
        timeout: '500ms', // request timeout
    };

    const res = http.post(url, null, params);

    check(res, {
        'status is OK': (r) => r.status === 200,
    });
    sleep(1);
}
