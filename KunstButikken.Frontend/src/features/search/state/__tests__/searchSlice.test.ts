import searchReducer, { setQuery, clearQuery } from '../searchSlice';

describe('searchSlice', () => {
  test('setQuery and clearQuery work', () => {
    let state = searchReducer(undefined, { type: 'unknown' } as any);
    expect(state.query).toBe('');
    state = searchReducer(state, setQuery('abc'));
    expect(state.query).toBe('abc');
    state = searchReducer(state, clearQuery());
    expect(state.query).toBe('');
  });
});

