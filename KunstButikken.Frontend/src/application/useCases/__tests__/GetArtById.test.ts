import 'reflect-metadata';
import { GetArtById } from '../GetArtById';

describe('GetArtById use case', () => {
  test('returns null for empty id', async () => {
    const repo: any = { getById: jest.fn() };
    const uc = new GetArtById(repo);
    const res = await uc.execute('');
    expect(res).toBeNull();
    expect(repo.getById).not.toHaveBeenCalled();
  });

  test('delegates to repository for non-empty id and returns value', async () => {
    const repo: any = { getById: jest.fn().mockResolvedValue({ id: 'x' }) };
    const uc = new GetArtById(repo);
    const res = await uc.execute('x');
    expect(repo.getById).toHaveBeenCalledWith('x');
    expect(res).toEqual({ id: 'x' });
  });

  test('propagates null from repo', async () => {
    const repo: any = { getById: jest.fn().mockResolvedValue(null) };
    const uc = new GetArtById(repo);
    const res = await uc.execute('y');
    expect(res).toBeNull();
  });
});
